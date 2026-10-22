using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;
using Serilog;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class AuditRepository : IAuditRepository
    {
        private const string InsertSql = @"INSERT INTO AuditLog (CustomerGUID, Source, Actor, Action, Target, Detail, ClientIP)
                VALUES (@CustomerGUID, @Source, @Actor, @Action, @Target, @Detail, @ClientIP)";

        // ILIKE over Actor, Action and Target; an empty search lists everything, newest first.
        private const string RecentSql = @"SELECT AuditLogID, CreateDate, Source, Actor, Action, Target, Detail, ClientIP
                FROM AuditLog
                WHERE CustomerGUID = @CustomerGUID
                  AND (@Search = '' OR Actor ILIKE @Pattern OR Action ILIKE @Pattern OR Target ILIKE @Pattern)
                ORDER BY CreateDate DESC, AuditLogID DESC
                LIMIT @Limit";

        private readonly IOptions<StorageOptions> _storageOptions;

        public AuditRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        public async Task Add(Guid customerGUID, string source, string actor, string action, string target, object detail, string clientIP = null)
        {
            try
            {
                await using DbConnection connection = CreateConnection();
                await connection.ExecuteAsync(InsertSql, new
                {
                    CustomerGUID = customerGUID,
                    Source = Truncate(source, 32),
                    Actor = Truncate(string.IsNullOrEmpty(actor) ? "unknown" : actor, 100),
                    Action = Truncate(action, 64),
                    Target = Truncate(target, 200),
                    Detail = detail == null ? null : detail as string ?? JsonSerializer.Serialize(detail),
                    ClientIP = Truncate(clientIP, 64)
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "audit_write_failed {AuditAction} {AuditActor} {AuditTarget}", action, actor, target);
            }
        }

        public async Task<IEnumerable<AuditEntry>> GetRecent(Guid customerGUID, int limit, string search)
        {
            search = (search ?? "").Trim();
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<AuditEntry>(RecentSql, new
            {
                CustomerGUID = customerGUID,
                Search = search,
                Pattern = "%" + search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%",
                Limit = Math.Clamp(limit, 1, 1000)
            });
        }

        private static string Truncate(string value, int maxLength)
        {
            return value == null || value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
