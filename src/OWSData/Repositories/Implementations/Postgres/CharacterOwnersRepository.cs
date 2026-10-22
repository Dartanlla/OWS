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
        // A redeemed ticket is claimed when the player's map has loaded; this only bounds how long one stays claimable.
        private const string ClaimTicketSql = @"UPDATE JoinTickets SET ClaimedAt = NOW()
                WHERE TicketGUID = @TicketGUID AND CustomerGUID = @CustomerGUID AND MapInstanceID = @MapInstanceID
                  AND RedeemedAt IS NOT NULL AND RedeemedAt > NOW() - INTERVAL '15 minutes'
                  AND ClaimedAt IS NULL AND CancelledAt IS NULL
                RETURNING CharacterID, CharacterName, StartMode";

        private const string CharacterByNameSql = @"SELECT CharacterID, CharName AS CharacterName FROM Characters
                WHERE CustomerGUID = @CustomerGUID AND CharName = @CharacterName";

        // Serializes the claim with the inventory and currency saves, which lock the same row.
        private const string LockCharacterSql = @"SELECT CharacterID FROM Characters
                WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID FOR UPDATE";

        private const string CurrentOwnerSql = @"SELECT OwnerToken FROM CharacterOwners
                WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID FOR UPDATE";

        private const string SetOwnerSql = @"INSERT INTO CharacterOwners (CustomerGUID, CharacterID, OwnerToken, MapInstanceID, ClaimKind, ClaimedAt)
                VALUES (@CustomerGUID, @CharacterID, @OwnerToken, @MapInstanceID, @ClaimKind, NOW())
                ON CONFLICT (CustomerGUID, CharacterID) DO UPDATE SET OwnerToken = EXCLUDED.OwnerToken,
                    MapInstanceID = EXCLUDED.MapInstanceID, ClaimKind = EXCLUDED.ClaimKind, ClaimedAt = EXCLUDED.ClaimedAt";

        private const string CancelTicketSql = @"UPDATE JoinTickets SET CancelledAt = NOW()
                WHERE TicketGUID = @TicketGUID AND CustomerGUID = @CustomerGUID AND ClaimedAt IS NULL AND CancelledAt IS NULL
                RETURNING TicketGUID";

        private const string TicketClaimedSql = @"SELECT ClaimedAt IS NOT NULL FROM JoinTickets
                WHERE TicketGUID = @TicketGUID AND CustomerGUID = @CustomerGUID";

        private const string StaleOwnersSql = @"SELECT P.Name AS CharacterName, P.Token AS OwnerToken,
                    CASE CO.ClaimKind WHEN 2 THEN 'travel' ELSE 'login' END AS Reason
                FROM unnest(@Names::text[], @Tokens::uuid[]) AS P(Name, Token)
                JOIN Characters C ON C.CustomerGUID = @CustomerGUID AND C.CharName = P.Name
                JOIN CharacterOwners CO ON CO.CustomerGUID = C.CustomerGUID AND CO.CharacterID = C.CharacterID
                WHERE CO.OwnerToken <> P.Token";

        private const string StaleByNameSql = @"SELECT EXISTS (SELECT 1 FROM CharacterOwners CO
                JOIN Characters C ON C.CustomerGUID = CO.CustomerGUID AND C.CharacterID = CO.CharacterID
                WHERE C.CustomerGUID = @CustomerGUID AND C.CharName = @CharacterName
                  AND CO.OwnerToken IS DISTINCT FROM @OwnerToken::uuid)";

        private const string StaleByIdSql = @"SELECT EXISTS (SELECT 1 FROM CharacterOwners
                WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID
                  AND OwnerToken IS DISTINCT FROM @OwnerToken::uuid)";

        private sealed class ClaimedTicketRow
        {
            public int CharacterID { get; set; }
            public string CharacterName { get; set; }
            public short StartMode { get; set; }
        }

        private sealed class CharacterRow
        {
            public int CharacterID { get; set; }
            public string CharacterName { get; set; }
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
                ticket.CharacterName, mapInstanceID, kind);
            await transaction.CommitAsync();
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
                character.CharacterName, mapInstanceID, CharacterClaimKind.Login);
            await transaction.CommitAsync();
            return result;
        }

        private static async Task<ClaimCharacterResult> SetOwner(DbConnection connection, DbTransaction transaction, Guid customerGUID,
            int characterID, string characterName, int mapInstanceID, CharacterClaimKind kind)
        {
            var key = new { CustomerGUID = customerGUID, CharacterID = characterID };
            await connection.ExecuteAsync(LockCharacterSql, key, transaction);
            Guid? replaced = await connection.QuerySingleOrDefaultAsync<Guid?>(CurrentOwnerSql, key, transaction);

            Guid ownerToken = Guid.NewGuid();
            await connection.ExecuteAsync(SetOwnerSql, new
            {
                CustomerGUID = customerGUID,
                CharacterID = characterID,
                OwnerToken = ownerToken,
                MapInstanceID = mapInstanceID,
                ClaimKind = (short)kind
            }, transaction);

            return new ClaimCharacterResult
            {
                Success = true,
                ErrorMessage = "",
                CharacterName = characterName,
                OwnerToken = ownerToken,
                ReplacedOwnerToken = replaced
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

            bool claimed = await connection.QuerySingleOrDefaultAsync<bool>(TicketClaimedSql, key);
            return new CancelJoinTicketResult { Success = true, ErrorMessage = "", Cancelled = false, Claimed = claimed };
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

            await using DbConnection connection = CreateConnection();
            IEnumerable<StaleCharacterOwner> stale = await connection.QueryAsync<StaleCharacterOwner>(StaleOwnersSql, new
            {
                CustomerGUID = customerGUID,
                Names = checks.Select(o => o.CharacterName).ToArray(),
                Tokens = checks.Select(o => o.OwnerToken).ToArray()
            });
            return stale.ToList();
        }

        public async Task<bool> IsStaleOwnerWrite(Guid customerGUID, string characterName, Guid? ownerToken)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteScalarAsync<bool>(StaleByNameSql, new
            {
                CustomerGUID = customerGUID,
                CharacterName = characterName,
                OwnerToken = ownerToken
            });
        }

        public async Task<bool> IsStaleOwnerWrite(Guid customerGUID, int characterID, Guid? ownerToken)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteScalarAsync<bool>(StaleByIdSql, new
            {
                CustomerGUID = customerGUID,
                CharacterID = characterID,
                OwnerToken = ownerToken
            });
        }
    }
}
