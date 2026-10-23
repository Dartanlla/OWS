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
    public class PlayerReportsRepository : IPlayerReportsRepository
    {
        private const int MaxReportsPerDay = 20;

        private const string Columns = @"R.ReportID, R.CreateDate, R.ReporterName, R.ReporterUserGUID, R.TargetName, R.TargetUserGUID, R.Reason,
                R.MapName, R.MapInstanceID, R.Details::TEXT AS Details, R.ChatExcerpt::TEXT AS ChatExcerpt, R.ResolvedAt, R.ResolvedBy,
                R.ResolutionNote,
                (SELECT COUNT(*) FROM PlayerReports T WHERE T.CustomerGUID = R.CustomerGUID AND LOWER(T.TargetName) = LOWER(R.TargetName)
                    AND T.CreateDate > NOW() - INTERVAL '30 days')::INT AS TargetReports30d,
                (SELECT COUNT(*) FROM PlayerReports P WHERE P.CustomerGUID = R.CustomerGUID AND LOWER(P.ReporterName) = LOWER(R.ReporterName)
                    AND P.CreateDate > NOW() - INTERVAL '30 days')::INT AS ReporterReports30d";

        private readonly IOptions<StorageOptions> _storageOptions;

        public PlayerReportsRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        private class CharacterOwner
        {
            public string CharName { get; set; }
            public Guid? UserGUID { get; set; }
        }

        public async Task<(long ReportID, string Error)> Add(Guid customerGUID, NewPlayerReport report)
        {
            string reporter = Truncate(report.ReporterName?.Trim(), 50);
            string target = Truncate(report.TargetName?.Trim(), 50);
            if (string.IsNullOrEmpty(reporter) || string.IsNullOrEmpty(target)) return (0, "unknown_character");

            await using DbConnection connection = CreateConnection();
            List<CharacterOwner> owners = (await connection.QueryAsync<CharacterOwner>(@"SELECT C.CharName, C.UserGUID FROM Characters C
                WHERE C.CustomerGUID = @CustomerGUID AND LOWER(C.CharName) IN (LOWER(@Reporter), LOWER(@Target))",
                new { CustomerGUID = customerGUID, Reporter = reporter, Target = target })).ToList();
            CharacterOwner reporterRow = owners.FirstOrDefault(o => string.Equals(o.CharName, reporter, StringComparison.OrdinalIgnoreCase));
            CharacterOwner targetRow = owners.FirstOrDefault(o => string.Equals(o.CharName, target, StringComparison.OrdinalIgnoreCase));
            if (reporterRow == null || targetRow == null) return (0, "unknown_character");

            int recent = await connection.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM PlayerReports
                WHERE CustomerGUID = @CustomerGUID AND LOWER(ReporterName) = LOWER(@Reporter) AND CreateDate > NOW() - INTERVAL '1 day'",
                new { CustomerGUID = customerGUID, Reporter = reporterRow.CharName });
            if (recent >= MaxReportsPerDay) return (0, "too_many");

            long id = await connection.ExecuteScalarAsync<long>(@"INSERT INTO PlayerReports
                (CustomerGUID, ReporterName, ReporterUserGUID, TargetName, TargetUserGUID, Reason, MapName, MapInstanceID, Details, ChatExcerpt)
                VALUES (@CustomerGUID, @Reporter, @ReporterUser, @Target, @TargetUser, @Reason, @MapName, @MapInstanceID,
                    COALESCE(SamsaraTryJsonb(@Details), '{}'::JSONB),
                    COALESCE((SELECT JSONB_AGG(JSONB_BUILD_OBJECT('at', L.CreateDate, 'channel', L.Channel, 'sender', L.SenderName,
                                  'to', L.TargetName, 'text', L.Message, 'dropped', L.Dropped) ORDER BY L.CreateDate)
                              FROM (SELECT * FROM ChatLog C
                                    WHERE C.CustomerGUID = @CustomerGUID AND C.CreateDate > NOW() - INTERVAL '10 minutes'
                                      AND (LOWER(C.SenderName) IN (LOWER(@Reporter), LOWER(@Target))
                                           OR LOWER(C.TargetName) IN (LOWER(@Reporter), LOWER(@Target)))
                                    ORDER BY C.CreateDate DESC LIMIT 100) L), '[]'::JSONB))
                RETURNING ReportID",
                new
                {
                    CustomerGUID = customerGUID,
                    Reporter = reporterRow.CharName,
                    ReporterUser = reporterRow.UserGUID,
                    Target = targetRow.CharName,
                    TargetUser = targetRow.UserGUID,
                    Reason = Truncate(report.Reason?.Trim() ?? "", 500),
                    MapName = Truncate(report.MapName, 50),
                    report.MapInstanceID,
                    Details = report.Details ?? "{}"
                });
            return (id, null);
        }

        public async Task<IEnumerable<PlayerReport>> List(Guid customerGUID, bool openOnly, int limit)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<PlayerReport>($@"SELECT {Columns} FROM PlayerReports R
                WHERE R.CustomerGUID = @CustomerGUID AND (NOT @OpenOnly OR R.ResolvedAt IS NULL)
                ORDER BY (R.ResolvedAt IS NULL) DESC, R.CreateDate DESC LIMIT @Limit",
                new { CustomerGUID = customerGUID, OpenOnly = openOnly, Limit = Math.Clamp(limit, 1, 500) });
        }

        public async Task<IEnumerable<PlayerReport>> ForCharacter(Guid customerGUID, string characterName, int limit)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<PlayerReport>($@"SELECT {Columns} FROM PlayerReports R
                WHERE R.CustomerGUID = @CustomerGUID AND (LOWER(R.TargetName) = LOWER(@Name) OR LOWER(R.ReporterName) = LOWER(@Name))
                ORDER BY R.CreateDate DESC LIMIT @Limit",
                new { CustomerGUID = customerGUID, Name = characterName?.Trim() ?? "", Limit = Math.Clamp(limit, 1, 200) });
        }

        public async Task<bool> Resolve(Guid customerGUID, long reportID, string resolvedBy, string note)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteAsync(@"UPDATE PlayerReports SET ResolvedAt = NOW(), ResolvedBy = @By, ResolutionNote = @Note
                WHERE CustomerGUID = @CustomerGUID AND ReportID = @ReportID AND ResolvedAt IS NULL",
                new { CustomerGUID = customerGUID, ReportID = reportID, By = Truncate(resolvedBy, 100), Note = Truncate(note?.Trim(), 1000) }) > 0;
        }

        public async Task<int> CountOpen(Guid customerGUID)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PlayerReports WHERE CustomerGUID = @CustomerGUID AND ResolvedAt IS NULL",
                new { CustomerGUID = customerGUID });
        }

        private static string Truncate(string value, int maxLength)
        {
            return string.IsNullOrEmpty(value) ? value : value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
