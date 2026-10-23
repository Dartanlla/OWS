using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class LoginQueueRepository : ILoginQueueRepository
    {
        private const string InWorldSql = @"EXISTS (SELECT 1 FROM CharOnMapInstance CMI
                INNER JOIN Characters C ON C.CustomerGUID = CMI.CustomerGUID AND C.CharacterID = CMI.CharacterID
                WHERE CMI.CustomerGUID = @CustomerGUID AND LOWER(C.CharName) = LOWER({0}))";

        private readonly IOptions<StorageOptions> _storageOptions;

        public LoginQueueRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        public async Task<int> Check(Guid customerGUID, Guid userSessionGUID, Guid userGUID, string characterName, int cap)
        {
            var args = new { CustomerGUID = customerGUID, Session = userSessionGUID, User = userGUID, Name = characterName ?? "", Cap = cap };

            await using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            await using DbTransaction transaction = await connection.BeginTransactionAsync();
            await connection.ExecuteAsync("SELECT pg_advisory_xact_lock(hashtext('login_queue:' || @CustomerGUID::TEXT))", args, transaction);

            bool returning = await connection.ExecuteScalarAsync<bool>($@"SELECT {string.Format(InWorldSql, "@Name")}
                OR EXISTS (SELECT 1 FROM GameEvents E WHERE E.CustomerGUID = @CustomerGUID AND E.CharacterName = @Name
                           AND E.Event = 'session_ended' AND E.EventTime > NOW() - INTERVAL '90 seconds')
                OR EXISTS (SELECT 1 FROM LoginQueue Q WHERE Q.CustomerGUID = @CustomerGUID AND Q.UserSessionGUID = @Session
                           AND Q.AdmittedAt > NOW() - INTERVAL '2 minutes')", args, transaction);
            if (returning)
            {
                await transaction.CommitAsync();
                return 0;
            }

            await connection.ExecuteAsync(@"INSERT INTO LoginQueue (CustomerGUID, UserSessionGUID, UserGUID, CharacterName)
                VALUES (@CustomerGUID, @Session, @User, @Name)
                ON CONFLICT (CustomerGUID, UserSessionGUID) DO UPDATE SET LastSeenAt = NOW(), CharacterName = EXCLUDED.CharacterName,
                    EnqueuedAt = CASE WHEN LoginQueue.LastSeenAt < NOW() - INTERVAL '30 seconds' THEN NOW() ELSE LoginQueue.EnqueuedAt END,
                    AdmittedAt = NULL", args, transaction);

            int taken = await connection.ExecuteScalarAsync<int>($@"SELECT
                (SELECT COUNT(*) FROM CharOnMapInstance WHERE CustomerGUID = @CustomerGUID)
                + (SELECT COUNT(*) FROM LoginQueue Q WHERE Q.CustomerGUID = @CustomerGUID AND Q.AdmittedAt > NOW() - INTERVAL '2 minutes'
                   AND NOT {string.Format(InWorldSql, "Q.CharacterName")})", args, transaction);
            int ahead = await connection.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM LoginQueue Q, LoginQueue Me
                WHERE Me.CustomerGUID = @CustomerGUID AND Me.UserSessionGUID = @Session
                  AND Q.CustomerGUID = @CustomerGUID AND Q.AdmittedAt IS NULL AND Q.LastSeenAt > NOW() - INTERVAL '30 seconds'
                  AND (Q.EnqueuedAt, Q.UserSessionGUID) < (Me.EnqueuedAt, Me.UserSessionGUID)", args, transaction);

            if (taken + ahead < cap)
            {
                await connection.ExecuteAsync("UPDATE LoginQueue SET AdmittedAt = NOW() WHERE CustomerGUID = @CustomerGUID AND UserSessionGUID = @Session",
                    args, transaction);
                await transaction.CommitAsync();
                return 0;
            }

            await transaction.CommitAsync();
            return ahead + 1;
        }

        public async Task<int> Waiting(Guid customerGUID)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM LoginQueue
                WHERE CustomerGUID = @CustomerGUID AND AdmittedAt IS NULL AND LastSeenAt > NOW() - INTERVAL '30 seconds'",
                new { CustomerGUID = customerGUID });
        }
    }
}
