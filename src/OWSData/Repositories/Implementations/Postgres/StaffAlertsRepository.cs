using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.Postgres
{
    /// <summary>
    /// The rows behind the staff Discord alerts, as one text line each. Each kind reads past a cursor (an ID, or a time
    /// for unbans) that the caller keeps. The box runs one customer, so these read every customer.
    /// </summary>
    public class StaffAlertsRepository
    {
        public class AlertRow
        {
            public long Cursor { get; set; }
            public string Text { get; set; }
        }

        private readonly IOptions<StorageOptions> _storageOptions;

        public StaffAlertsRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        /// <summary>The cursor each kind starts from, so a first start does not post history.</summary>
        public async Task<long> CurrentCursor(string kind)
        {
            string sql = kind switch
            {
                "reports" => "SELECT COALESCE(MAX(ReportID), 0) FROM PlayerReports",
                "bans" => "SELECT COALESCE(MAX(SanctionID), 0) FROM AccountSanctions",
                "unbans" => "SELECT (EXTRACT(EPOCH FROM NOW()) * 1000)::BIGINT",
                "gifts" => "SELECT COALESCE(MAX(GameEventID), 0) FROM GameEvents",
                "zone_exits" => "SELECT COALESCE(MAX(AuditLogID), 0) FROM AuditLog",
                _ => "SELECT 0"
            };
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteScalarAsync<long>(sql);
        }

        public async Task<List<AlertRow>> Read(string kind, long after)
        {
            string sql = kind switch
            {
                "reports" => @"SELECT ReportID AS Cursor,
                        'Report: ' || ReporterName || ' reported ' || TargetName || COALESCE(' on ' || MapName, '') || ': ' || LEFT(Reason, 200) AS Text
                    FROM PlayerReports WHERE ReportID > @After ORDER BY ReportID LIMIT 50",
                "bans" => @"SELECT S.SanctionID AS Cursor,
                        'Ban: ' || COALESCE(S.CharacterName, (SELECT C.CharName FROM Characters C WHERE C.UserGUID = S.UserGUID
                                                              ORDER BY C.LastActivity DESC LIMIT 1), 'an account')
                        || ' by ' || S.IssuedBy
                        || CASE WHEN S.ExpiresAt IS NULL THEN ', permanent' ELSE ' until ' || TO_CHAR(S.ExpiresAt AT TIME ZONE 'UTC', 'YYYY-MM-DD HH24:MI') || ' UTC' END
                        || CASE WHEN S.Reason <> '' THEN '. Reason: ' || LEFT(S.Reason, 200) ELSE '' END AS Text
                    FROM AccountSanctions S WHERE S.SanctionID > @After AND S.Kind = 'ban' ORDER BY S.SanctionID LIMIT 50",
                "unbans" => @"SELECT (EXTRACT(EPOCH FROM S.RevokedAt) * 1000)::BIGINT AS Cursor,
                        'Unban: ' || COALESCE(S.CharacterName, 'an account') || ' by ' || COALESCE(S.RevokedBy, '?') AS Text
                    FROM AccountSanctions S
                    WHERE S.Kind = 'ban' AND S.RevokedAt > TO_TIMESTAMP(@After / 1000.0) ORDER BY S.RevokedAt LIMIT 50",
                "gifts" => @"SELECT GameEventID AS Cursor,
                        'Staff gift: ' || COALESCE(CharacterName, '?') || ' got '
                        || CASE WHEN Event = 'currency_changed' THEN COALESCE(Fields ->> 'delta', '?') || ' juri'
                                ELSE COALESCE(Fields ->> 'qty', '1') || ' x ' || COALESCE(Fields ->> 'item', '?') END AS Text
                    FROM GameEvents
                    WHERE GameEventID > @After AND Event IN ('item_gained', 'currency_changed') AND Fields ->> 'source' = 'staff'
                      AND (Event = 'item_gained' OR ABS(NULLIF(Fields ->> 'delta', '')::NUMERIC) >= @LargeJuriChange)
                      AND EventTime > NOW() - INTERVAL '1 hour'
                    ORDER BY GameEventID LIMIT 50",
                "zone_exits" => @"SELECT AuditLogID AS Cursor,
                        'Zone server stopped unexpectedly: ' || COALESCE(Target, '?') || ' (exit code '
                        || COALESCE(SamsaraTryJsonb(Detail) ->> 'exitCode', '?') || ')' AS Text
                    FROM AuditLog WHERE AuditLogID > @After AND Action = 'zone_exit' ORDER BY AuditLogID LIMIT 50",
                _ => null
            };
            if (sql == null) return new List<AlertRow>();

            await using DbConnection connection = CreateConnection();
            return (await connection.QueryAsync<AlertRow>(sql, new { After = after, ConsoleQueriesRepository.LargeJuriChange })).ToList();
        }

        /// <summary>Failed saves in the last 10 minutes and the endpoints that failed.</summary>
        public async Task<(int Count, string Endpoints)> FailedSaves()
        {
            await using DbConnection connection = CreateConnection();
            var row = await connection.QueryFirstAsync<(long Count, string Endpoints)>(@"SELECT COUNT(*),
                    COALESCE(STRING_AGG(DISTINCT Fields ->> 'endpoint', ', '), '')
                FROM GameEvents WHERE Event = 'persistence_failed' AND EventTime > NOW() - INTERVAL '10 minutes'");
            return ((int)row.Count, row.Endpoints);
        }

        /// <summary>Item instance IDs held more than once, with the characters that hold them.</summary>
        public async Task<List<(string InstanceId, string Holders)>> Duplicates()
        {
            await using DbConnection connection = CreateConnection();
            return (await connection.QueryAsync<(string InstanceId, string Holders)>(@"SELECT CII.InstanceId::TEXT,
                    STRING_AGG(DISTINCT C.CharName, ', ')
                FROM CharInventoryItems CII
                INNER JOIN CharInventory CI ON CI.CustomerGUID = CII.CustomerGUID AND CI.CharInventoryID = CII.CharInventoryID
                INNER JOIN Characters C ON C.CustomerGUID = CI.CustomerGUID AND C.CharacterID = CI.CharacterID
                WHERE CII.InstanceId IS NOT NULL AND CII.Quantity = 1
                GROUP BY CII.InstanceId HAVING COUNT(*) > 1
                ORDER BY CII.InstanceId LIMIT 200")).ToList();
        }
    }
}
