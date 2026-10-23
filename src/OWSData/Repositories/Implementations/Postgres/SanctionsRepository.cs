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
    public class SanctionsRepository : ISanctionsRepository
    {
        private const string Columns = @"S.SanctionID, S.UserGUID, S.Kind, S.Reason, S.CharacterName, S.CreateDate, S.ExpiresAt, S.IssuedBy,
                S.IssuedFrom, S.RevokedAt, S.RevokedBy,
                (S.Kind IN ('ban', 'mute') AND S.RevokedAt IS NULL AND (S.ExpiresAt IS NULL OR S.ExpiresAt > NOW())) AS Active";

        private const string OpenFilter = "S.RevokedAt IS NULL AND (S.ExpiresAt IS NULL OR S.ExpiresAt > NOW())";

        private readonly IOptions<StorageOptions> _storageOptions;

        public SanctionsRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        public async Task<ActiveSanctions> GetActive(Guid customerGUID, Guid userGUID)
        {
            await using DbConnection connection = CreateConnection();
            List<Sanction> open = (await connection.QueryAsync<Sanction>($@"SELECT {Columns} FROM AccountSanctions S
                WHERE S.CustomerGUID = @CustomerGUID AND S.UserGUID = @UserGUID AND S.Kind IN ('ban', 'mute') AND {OpenFilter}
                ORDER BY S.CreateDate DESC", new { CustomerGUID = customerGUID, UserGUID = userGUID })).ToList();

            return new ActiveSanctions
            {
                Ban = open.FirstOrDefault(s => s.Kind == SanctionKinds.Ban),
                Mute = open.FirstOrDefault(s => s.Kind == SanctionKinds.Mute)
            };
        }

        public async Task<IEnumerable<Sanction>> GetForUser(Guid customerGUID, Guid userGUID)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<Sanction>($@"SELECT {Columns} FROM AccountSanctions S
                WHERE S.CustomerGUID = @CustomerGUID AND S.UserGUID = @UserGUID
                ORDER BY S.CreateDate DESC LIMIT 200", new { CustomerGUID = customerGUID, UserGUID = userGUID });
        }

        public async Task<Sanction> GetById(Guid customerGUID, Guid userGUID, long sanctionID)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Sanction>($@"SELECT {Columns} FROM AccountSanctions S
                WHERE S.CustomerGUID = @CustomerGUID AND S.UserGUID = @UserGUID AND S.SanctionID = @SanctionID",
                new { CustomerGUID = customerGUID, UserGUID = userGUID, SanctionID = sanctionID });
        }

        public async Task<IEnumerable<Sanction>> GetRecent(Guid customerGUID, bool activeOnly, int limit)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<Sanction>($@"SELECT {Columns}, U.FirstName AS AccountName
                FROM AccountSanctions S
                LEFT JOIN Users U ON U.UserGUID = S.UserGUID
                WHERE S.CustomerGUID = @CustomerGUID
                  AND (NOT @ActiveOnly OR (S.Kind IN ('ban', 'mute') AND {OpenFilter}))
                ORDER BY S.CreateDate DESC LIMIT @Limit",
                new { CustomerGUID = customerGUID, ActiveOnly = activeOnly, Limit = Math.Clamp(limit, 1, 500) });
        }

        public async Task<Guid?> GetUserForCharacter(Guid customerGUID, string characterName)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Guid?>(@"SELECT UserGUID FROM Characters
                WHERE CustomerGUID = @CustomerGUID AND LOWER(CharName) = LOWER(@CharName) LIMIT 1",
                new { CustomerGUID = customerGUID, CharName = characterName?.Trim() ?? "" });
        }

        public async Task<long> Add(Guid customerGUID, Guid userGUID, string kind, string reason, string characterName, DateTime? expiresAtUtc,
            string issuedBy, string issuedFrom)
        {
            await using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            await using DbTransaction tx = await connection.BeginTransactionAsync();

            if (SanctionKinds.HasDuration(kind))
            {
                // Two staff sanctioning the same account at once would otherwise both leave an open row.
                await connection.ExecuteAsync("SELECT pg_advisory_xact_lock(hashtext('sanction:' || @UserGUID::text))",
                    new { UserGUID = userGUID }, tx);
                await connection.ExecuteAsync(@"UPDATE AccountSanctions S SET RevokedAt = NOW(), RevokedBy = @RevokedBy
                    WHERE S.CustomerGUID = @CustomerGUID AND S.UserGUID = @UserGUID AND S.Kind = @Kind AND S.RevokedAt IS NULL",
                    new { CustomerGUID = customerGUID, UserGUID = userGUID, Kind = kind, RevokedBy = Truncate(issuedBy, 100) }, tx);
            }

            long id = await connection.ExecuteScalarAsync<long>(@"INSERT INTO AccountSanctions
                (CustomerGUID, UserGUID, Kind, Reason, CharacterName, ExpiresAt, IssuedBy, IssuedFrom)
                VALUES (@CustomerGUID, @UserGUID, @Kind, @Reason, @CharacterName, @ExpiresAt, @IssuedBy, @IssuedFrom)
                RETURNING SanctionID",
                new
                {
                    CustomerGUID = customerGUID,
                    UserGUID = userGUID,
                    Kind = kind,
                    Reason = reason?.Trim() ?? "",
                    CharacterName = Truncate(characterName, 50),
                    ExpiresAt = SanctionKinds.HasDuration(kind) && expiresAtUtc.HasValue ? DateTime.SpecifyKind(expiresAtUtc.Value, DateTimeKind.Utc) : (DateTime?)null,
                    IssuedBy = Truncate(issuedBy, 100),
                    IssuedFrom = issuedFrom
                }, tx);

            await tx.CommitAsync();
            return id;
        }

        public async Task<int> RevokeActive(Guid customerGUID, Guid userGUID, string kind, string revokedBy)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteAsync($@"UPDATE AccountSanctions S SET RevokedAt = NOW(), RevokedBy = @RevokedBy
                WHERE S.CustomerGUID = @CustomerGUID AND S.UserGUID = @UserGUID AND S.Kind = @Kind AND {OpenFilter}",
                new { CustomerGUID = customerGUID, UserGUID = userGUID, Kind = kind, RevokedBy = Truncate(revokedBy, 100) });
        }

        public async Task<bool> Revoke(Guid customerGUID, long sanctionID, string revokedBy)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteAsync(@"UPDATE AccountSanctions SET RevokedAt = NOW(), RevokedBy = @RevokedBy
                WHERE CustomerGUID = @CustomerGUID AND SanctionID = @SanctionID AND RevokedAt IS NULL",
                new { CustomerGUID = customerGUID, SanctionID = sanctionID, RevokedBy = Truncate(revokedBy, 100) }) > 0;
        }

        private static string Truncate(string value, int maxLength)
        {
            return value == null || value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
