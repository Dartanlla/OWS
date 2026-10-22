using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class CharacterOwnersRepository : ICharacterOwnersRepository
    {
        // A zone server lets a character trade or drop items only this long after CheckOwners last confirmed it.
        private const int OwnershipLeaseSeconds = 8;
        // How long a replaced live owner's saves still land, and how long the claiming zone server waits to load.
        // The old copy stops transfers within the lease, so its last saves have the rest of this time to arrive.
        private const int HandoverSeconds = 20;

        // A redeemed ticket is claimed when the player's map has loaded; this only bounds how long one stays claimable.
        private const string ClaimTicketSql = @"UPDATE JoinTickets SET ClaimedAt = NOW()
                WHERE TicketGUID = @TicketGUID AND CustomerGUID = @CustomerGUID AND MapInstanceID = @MapInstanceID
                  AND RedeemedAt IS NOT NULL AND RedeemedAt > NOW() - INTERVAL '15 minutes'
                  AND ClaimedAt IS NULL AND CancelledAt IS NULL
                RETURNING CharacterID, CharacterName, StartMode, HandoffOwnerToken";

        private const string CharacterByNameSql = @"SELECT CharacterID, CharName AS CharacterName FROM Characters
                WHERE CustomerGUID = @CustomerGUID AND CharName = @CharacterName";

        // Serializes the claim with the inventory and currency saves, which lock the same row.
        private const string LockCharacterSql = @"SELECT CharacterID FROM Characters
                WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID FOR UPDATE";

        private const string CurrentOwnerSql = @"SELECT OwnerToken,
                    COALESCE(LastConfirmedAt > NOW() - make_interval(secs => @LeaseSeconds), false) AS IsLive,
                    COALESCE(PreviousOwnerUntil > NOW(), false) AS IsHandingOver
                FROM CharacterOwners
                WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID FOR UPDATE";

        // The new owner counts as confirmed from its claim, so a later claim hands over from it too.
        private const string SetOwnerSql = @"INSERT INTO CharacterOwners (CustomerGUID, CharacterID, OwnerToken, MapInstanceID, ClaimKind, ClaimedAt,
                    LastConfirmedAt, PreviousOwnerToken, PreviousOwnerUntil)
                VALUES (@CustomerGUID, @CharacterID, @OwnerToken, @MapInstanceID, @ClaimKind, NOW(), NOW(), @PreviousOwnerToken,
                    CASE WHEN @PreviousOwnerToken::uuid IS NULL THEN NULL ELSE NOW() + make_interval(secs => @HandoverSeconds) END)
                ON CONFLICT (CustomerGUID, CharacterID) DO UPDATE SET OwnerToken = EXCLUDED.OwnerToken,
                    MapInstanceID = EXCLUDED.MapInstanceID, ClaimKind = EXCLUDED.ClaimKind, ClaimedAt = EXCLUDED.ClaimedAt,
                    LastConfirmedAt = EXCLUDED.LastConfirmedAt, PreviousOwnerToken = EXCLUDED.PreviousOwnerToken,
                    PreviousOwnerUntil = EXCLUDED.PreviousOwnerUntil";

        private const string CancelTicketSql = @"UPDATE JoinTickets SET CancelledAt = NOW()
                WHERE TicketGUID = @TicketGUID AND CustomerGUID = @CustomerGUID AND ClaimedAt IS NULL AND CancelledAt IS NULL
                RETURNING TicketGUID";

        private const string TicketClaimedSql = @"SELECT ClaimedAt IS NOT NULL FROM JoinTickets
                WHERE TicketGUID = @TicketGUID AND CustomerGUID = @CustomerGUID";

        // A replaced token is reported at once, even while its saves still land during a handover.
        private const string StaleOwnersSql = @"SELECT P.Name AS CharacterName, P.Token AS OwnerToken,
                    CASE CO.ClaimKind WHEN 2 THEN 'travel' ELSE 'login' END AS Reason
                FROM unnest(@Names::text[], @Tokens::uuid[]) AS P(Name, Token)
                JOIN Characters C ON C.CustomerGUID = @CustomerGUID AND C.CharName = P.Name
                JOIN CharacterOwners CO ON CO.CustomerGUID = C.CustomerGUID AND CO.CharacterID = C.CharacterID
                WHERE CO.OwnerToken <> P.Token";

        private const string ConfirmOwnersSql = @"UPDATE CharacterOwners CO SET LastConfirmedAt = NOW()
                FROM unnest(@Names::text[], @Tokens::uuid[]) AS P(Name, Token)
                JOIN Characters C ON C.CustomerGUID = @CustomerGUID AND C.CharName = P.Name
                WHERE CO.CustomerGUID = C.CustomerGUID AND CO.CharacterID = C.CharacterID AND CO.OwnerToken = P.Token";

        // The replaced owner of a running handover may still write, except where AllowPrevious is false (logout).
        private const string NotOwnerSql = @"CO.OwnerToken IS DISTINCT FROM @OwnerToken::uuid
                AND NOT (@AllowPrevious AND COALESCE(CO.PreviousOwnerToken = @OwnerToken::uuid AND CO.PreviousOwnerUntil > NOW(), false))";

        private const string StaleByNameSql = @"SELECT EXISTS (SELECT 1 FROM CharacterOwners CO
                JOIN Characters C ON C.CustomerGUID = CO.CustomerGUID AND C.CharacterID = CO.CharacterID
                WHERE C.CustomerGUID = @CustomerGUID AND C.CharName = @CharacterName AND " + NotOwnerSql + ")";

        private const string StaleByIdSql = @"SELECT EXISTS (SELECT 1 FROM CharacterOwners CO
                WHERE CO.CustomerGUID = @CustomerGUID AND CO.CharacterID = @CharacterID AND " + NotOwnerSql + ")";

        private const string EndOwnerSessionSql = @"UPDATE CharacterOwners CO SET LastConfirmedAt = NULL
                FROM Characters C
                WHERE C.CustomerGUID = @CustomerGUID AND C.CharName = @CharacterName
                  AND CO.CustomerGUID = C.CustomerGUID AND CO.CharacterID = C.CharacterID AND CO.OwnerToken = @OwnerToken::uuid";

        private sealed class ClaimedTicketRow
        {
            public int CharacterID { get; set; }
            public string CharacterName { get; set; }
            public short StartMode { get; set; }
            public Guid? HandoffOwnerToken { get; set; }
        }

        private sealed class CharacterRow
        {
            public int CharacterID { get; set; }
            public string CharacterName { get; set; }
        }

        private sealed class OwnerRow
        {
            public Guid OwnerToken { get; set; }
            public bool IsLive { get; set; }
            public bool IsHandingOver { get; set; }
        }

        private readonly IOptions<StorageOptions> _storageOptions;

        public CharacterOwnersRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        public async Task<ClaimCharacterResult> ClaimWithTicket(Guid customerGUID, Guid ticketGUID, int mapInstanceID)
        {
            await using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            await using DbTransaction transaction = await connection.BeginTransactionAsync();

            ClaimedTicketRow ticket = await connection.QuerySingleOrDefaultAsync<ClaimedTicketRow>(ClaimTicketSql, new
            {
                TicketGUID = ticketGUID,
                CustomerGUID = customerGUID,
                MapInstanceID = mapInstanceID
            }, transaction);
            if (ticket == null)
            {
                return new ClaimCharacterResult { Success = false, ErrorMessage = "ticket_refused" };
            }

            // Only a zone server sets a start (portal, staff teleport); a client's login ticket uses the stored position.
            CharacterClaimKind kind = ticket.StartMode == (short)JoinTicketStartMode.StoredPosition
                ? CharacterClaimKind.Login
                : CharacterClaimKind.Travel;
            ClaimCharacterResult result = await SetOwner(connection, transaction, customerGUID, ticket.CharacterID,
                ticket.CharacterName, mapInstanceID, kind, ticket.HandoffOwnerToken);
            if (result.Success)
            {
                await transaction.CommitAsync();
            }
            return result;
        }

        public async Task<ClaimCharacterResult> ClaimByName(Guid customerGUID, string characterName, int mapInstanceID)
        {
            await using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            await using DbTransaction transaction = await connection.BeginTransactionAsync();

            CharacterRow character = await connection.QuerySingleOrDefaultAsync<CharacterRow>(CharacterByNameSql, new
            {
                CustomerGUID = customerGUID,
                CharacterName = characterName
            }, transaction);
            if (character == null)
            {
                return new ClaimCharacterResult { Success = false, ErrorMessage = "character_not_found" };
            }

            ClaimCharacterResult result = await SetOwner(connection, transaction, customerGUID, character.CharacterID,
                character.CharacterName, mapInstanceID, CharacterClaimKind.Login, null);
            if (result.Success)
            {
                await transaction.CommitAsync();
            }
            return result;
        }

        private static async Task<ClaimCharacterResult> SetOwner(DbConnection connection, DbTransaction transaction, Guid customerGUID,
            int characterID, string characterName, int mapInstanceID, CharacterClaimKind kind, Guid? handoffOwnerToken)
        {
            await connection.ExecuteAsync(LockCharacterSql, new { CustomerGUID = customerGUID, CharacterID = characterID }, transaction);
            OwnerRow current = await connection.QuerySingleOrDefaultAsync<OwnerRow>(CurrentOwnerSql, new
            {
                CustomerGUID = customerGUID,
                CharacterID = characterID,
                LeaseSeconds = OwnershipLeaseSeconds
            }, transaction);

            // One handover at a time: a second one would end the first copy's save window early.
            if (current != null && current.IsHandingOver)
            {
                return new ClaimCharacterResult { Success = false, ErrorMessage = "handover_running" };
            }

            bool handOver = current != null && current.IsLive && current.OwnerToken != handoffOwnerToken;
            Guid ownerToken = Guid.NewGuid();
            await connection.ExecuteAsync(SetOwnerSql, new
            {
                CustomerGUID = customerGUID,
                CharacterID = characterID,
                OwnerToken = ownerToken,
                MapInstanceID = mapInstanceID,
                ClaimKind = (short)kind,
                PreviousOwnerToken = handOver ? current.OwnerToken : (Guid?)null,
                HandoverSeconds
            }, transaction);

            return new ClaimCharacterResult
            {
                Success = true,
                ErrorMessage = "",
                CharacterName = characterName,
                OwnerToken = ownerToken,
                ReplacedOwnerToken = current?.OwnerToken,
                HandoverSeconds = handOver ? HandoverSeconds : 0
            };
        }

        public async Task<CancelJoinTicketResult> CancelTicket(Guid customerGUID, Guid ticketGUID)
        {
            var key = new { TicketGUID = ticketGUID, CustomerGUID = customerGUID };
            await using DbConnection connection = CreateConnection();
            Guid? cancelled = await connection.QuerySingleOrDefaultAsync<Guid?>(CancelTicketSql, key);
            if (cancelled.HasValue)
            {
                return new CancelJoinTicketResult { Success = true, ErrorMessage = "", Cancelled = true };
            }

            // Already void, or unknown: neither can be claimed, so a retried cancel still reads as cancelled.
            bool claimed = await connection.QuerySingleOrDefaultAsync<bool>(TicketClaimedSql, key);
            return new CancelJoinTicketResult { Success = true, ErrorMessage = "", Cancelled = !claimed, Claimed = claimed };
        }

        public async Task<List<StaleCharacterOwner>> GetStaleOwners(Guid customerGUID, IEnumerable<CharacterOwnerCheck> owners)
        {
            List<CharacterOwnerCheck> checks = (owners ?? Enumerable.Empty<CharacterOwnerCheck>())
                .Where(o => !string.IsNullOrEmpty(o?.CharacterName))
                .ToList();
            if (checks.Count == 0)
            {
                return new List<StaleCharacterOwner>();
            }

            var parameters = new
            {
                CustomerGUID = customerGUID,
                Names = checks.Select(o => o.CharacterName).ToArray(),
                Tokens = checks.Select(o => o.OwnerToken).ToArray()
            };
            await using DbConnection connection = CreateConnection();
            await connection.ExecuteAsync(ConfirmOwnersSql, parameters);
            IEnumerable<StaleCharacterOwner> stale = await connection.QueryAsync<StaleCharacterOwner>(StaleOwnersSql, parameters);
            return stale.ToList();
        }

        public async Task<bool> IsStaleOwnerWrite(Guid customerGUID, string characterName, Guid? ownerToken, bool allowPreviousOwner = true)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteScalarAsync<bool>(StaleByNameSql, new
            {
                CustomerGUID = customerGUID,
                CharacterName = characterName,
                OwnerToken = ownerToken,
                AllowPrevious = allowPreviousOwner
            });
        }

        public async Task<bool> IsStaleOwnerWrite(Guid customerGUID, int characterID, Guid? ownerToken)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteScalarAsync<bool>(StaleByIdSql, new
            {
                CustomerGUID = customerGUID,
                CharacterID = characterID,
                OwnerToken = ownerToken,
                AllowPrevious = true
            });
        }

        public async Task EndOwnerSession(Guid customerGUID, string characterName, Guid? ownerToken)
        {
            if (!ownerToken.HasValue)
            {
                return;
            }

            await using DbConnection connection = CreateConnection();
            await connection.ExecuteAsync(EndOwnerSessionSql, new
            {
                CustomerGUID = customerGUID,
                CharacterName = characterName,
                OwnerToken = ownerToken
            });
        }
    }
}
