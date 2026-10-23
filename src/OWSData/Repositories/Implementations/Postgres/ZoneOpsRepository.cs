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
    public class ZoneOpsRepository : IZoneOpsRepository
    {
        public const string RetryResult = "retry";

        private const string CommandColumns = @"CommandID, MapInstanceID, CharacterName, Command, Args, CreateDate, CreatedBy, ExpiresAt,
                DeliveredAt, DeliveredToMapInstanceID, DeliveryAttempt, CompletedAt, Result, ResultDetail";

        private const string TakePendingSql = @"WITH Due AS (
                    SELECT ZC.CommandID
                    FROM ZoneCommands ZC
                    WHERE ZC.CustomerGUID = @CustomerGUID
                      AND ZC.CompletedAt IS NULL
                      AND ZC.ExpiresAt > NOW()
                      AND (ZC.DeliveredAt IS NULL
                           OR (ZC.Command NOT IN ('give_item', 'give_juri')
                               AND (ZC.DeliveredAt < NOW() - INTERVAL '60 seconds' OR ZC.DeliveredToMapInstanceID IS DISTINCT FROM @MapInstanceID)))
                      AND (ZC.MapInstanceID = @MapInstanceID
                           OR (ZC.MapInstanceID IS NULL AND EXISTS (
                               SELECT 1 FROM CharOnMapInstance CMI
                               INNER JOIN Characters C ON C.CustomerGUID = CMI.CustomerGUID AND C.CharacterID = CMI.CharacterID
                               WHERE CMI.CustomerGUID = ZC.CustomerGUID AND CMI.MapInstanceID = @MapInstanceID
                                 AND LOWER(C.CharName) = LOWER(ZC.CharacterName))))
                    ORDER BY ZC.CommandID
                    LIMIT 50
                    FOR UPDATE OF ZC SKIP LOCKED)
                UPDATE ZoneCommands Z SET DeliveredAt = NOW(), DeliveredToMapInstanceID = @MapInstanceID, DeliveryAttempt = Z.DeliveryAttempt + 1
                FROM Due WHERE Z.CommandID = Due.CommandID
                RETURNING Z.CommandID, Z.MapInstanceID, Z.CharacterName, Z.Command, Z.Args, Z.CreateDate, Z.CreatedBy, Z.ExpiresAt,
                    Z.DeliveredAt, Z.DeliveredToMapInstanceID, Z.DeliveryAttempt, Z.CompletedAt, Z.Result, Z.ResultDetail";

        private readonly IOptions<StorageOptions> _storageOptions;

        public ZoneOpsRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        public async Task<IEnumerable<ZoneCommand>> TakePendingCommands(Guid customerGUID, int mapInstanceID)
        {
            await using DbConnection connection = CreateConnection();
            return (await connection.QueryAsync<ZoneCommand>(TakePendingSql, new { CustomerGUID = customerGUID, MapInstanceID = mapInstanceID }))
                .OrderBy(command => command.CommandID);
        }

        public async Task CompleteCommands(Guid customerGUID, int mapInstanceID, IEnumerable<ZoneCommandResult> results)
        {
            List<ZoneCommandResult> finished = results?.Where(r => r != null && r.CommandId > 0 && !string.IsNullOrWhiteSpace(r.Result)).ToList();
            if (finished == null || finished.Count == 0) return;

            await using DbConnection connection = CreateConnection();
            foreach (ZoneCommandResult result in finished)
            {
                if (result.Result == RetryResult)
                {
                    // Released for the next poll, only by the zone that holds this delivery: a gift is otherwise never sent twice.
                    await connection.ExecuteAsync(@"UPDATE ZoneCommands SET DeliveredAt = NULL, DeliveredToMapInstanceID = NULL, ResultDetail = @Detail
                        WHERE CustomerGUID = @CustomerGUID AND CommandID = @CommandID AND CompletedAt IS NULL
                          AND DeliveredToMapInstanceID = @MapInstanceID AND DeliveryAttempt = @Attempt",
                        new { CustomerGUID = customerGUID, CommandID = result.CommandId, MapInstanceID = mapInstanceID, Attempt = result.Attempt, Detail = Truncate(result.Detail, 2000) });
                    continue;
                }

                await connection.ExecuteAsync(@"UPDATE ZoneCommands SET CompletedAt = NOW(), Result = @Result, ResultDetail = @Detail
                    WHERE CustomerGUID = @CustomerGUID AND CommandID = @CommandID AND CompletedAt IS NULL",
                    new
                    {
                        CustomerGUID = customerGUID,
                        CommandID = result.CommandId,
                        Result = Truncate(result.Result, 16),
                        Detail = Truncate(result.Detail, 2000)
                    });
            }
        }

        public async Task<long> AddCommand(Guid customerGUID, int? mapInstanceID, string characterName, string command, string args, string createdBy, TimeSpan lifetime)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteScalarAsync<long>(@"INSERT INTO ZoneCommands
                (CustomerGUID, MapInstanceID, CharacterName, Command, Args, CreatedBy, ExpiresAt)
                VALUES (@CustomerGUID, @MapInstanceID, @CharacterName, @Command, @Args, @CreatedBy, NOW() + make_interval(secs => @LifetimeSeconds))
                RETURNING CommandID",
                new
                {
                    CustomerGUID = customerGUID,
                    MapInstanceID = mapInstanceID,
                    CharacterName = Truncate(characterName?.Trim(), 50),
                    Command = command,
                    Args = string.IsNullOrEmpty(args) ? "{}" : args,
                    CreatedBy = Truncate(createdBy, 100),
                    LifetimeSeconds = lifetime.TotalSeconds
                });
        }

        public async Task<int> AddCommandForAllInstances(Guid customerGUID, string command, string args, string createdBy, TimeSpan lifetime)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteAsync(@"INSERT INTO ZoneCommands (CustomerGUID, MapInstanceID, Command, Args, CreatedBy, ExpiresAt)
                SELECT @CustomerGUID, MI.MapInstanceID, @Command, @Args, @CreatedBy, NOW() + make_interval(secs => @LifetimeSeconds)
                FROM MapInstances MI
                WHERE MI.CustomerGUID = @CustomerGUID AND MI.Status = 2",
                new
                {
                    CustomerGUID = customerGUID,
                    Command = command,
                    Args = string.IsNullOrEmpty(args) ? "{}" : args,
                    CreatedBy = Truncate(createdBy, 100),
                    LifetimeSeconds = lifetime.TotalSeconds
                });
        }

        public async Task<IEnumerable<ZoneCommand>> GetRecentCommands(Guid customerGUID, string characterName, int limit)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<ZoneCommand>($@"SELECT {CommandColumns} FROM ZoneCommands
                WHERE CustomerGUID = @CustomerGUID AND (@CharacterName = '' OR LOWER(CharacterName) = LOWER(@CharacterName))
                ORDER BY CommandID DESC LIMIT @Limit",
                new { CustomerGUID = customerGUID, CharacterName = characterName?.Trim() ?? "", Limit = Math.Clamp(limit, 1, 500) });
        }

        public async Task<int> AddEvents(Guid customerGUID, int mapInstanceID, IReadOnlyCollection<GameEventIn> events)
        {
            if (events == null || events.Count == 0) return 0;

            await using DbConnection connection = CreateConnection();
            return await connection.ExecuteAsync(@"INSERT INTO GameEvents (CustomerGUID, EventGUID, EventTime, MapInstanceID, MapName, CharacterName, Event, Fields)
                SELECT @CustomerGUID, E.EventGUID, E.EventTime, @MapInstanceID, E.MapName, E.CharacterName, E.Event, E.Fields::JSONB
                FROM UNNEST(@EventGUIDs, @EventTimes, @MapNames, @CharacterNames, @Events, @Fields)
                    AS E(EventGUID, EventTime, MapName, CharacterName, Event, Fields)
                ON CONFLICT (EventGUID) DO NOTHING",
                new
                {
                    CustomerGUID = customerGUID,
                    MapInstanceID = mapInstanceID,
                    EventGUIDs = events.Select(e => e.EventGUID).ToArray(),
                    EventTimes = events.Select(e => DateTime.SpecifyKind(e.EventTime, DateTimeKind.Utc)).ToArray(),
                    MapNames = events.Select(e => e.MapName).ToArray(),
                    CharacterNames = events.Select(e => e.CharacterName).ToArray(),
                    Events = events.Select(e => e.Event).ToArray(),
                    Fields = events.Select(e => e.Fields).ToArray()
                });
        }

        public async Task<IEnumerable<GameEventRow>> GetEvents(Guid customerGUID, string characterName, string eventName, long beforeID, int limit)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<GameEventRow>(@"SELECT GameEventID, EventTime, MapInstanceID, MapName, CharacterName, Event, Fields::TEXT AS Fields
                FROM GameEvents
                WHERE CustomerGUID = @CustomerGUID
                  AND (@CharacterName = '' OR CharacterName = @CharacterName)
                  AND (@Event = '' OR Event = @Event)
                  AND (@BeforeID <= 0 OR GameEventID < @BeforeID)
                ORDER BY GameEventID DESC LIMIT @Limit",
                new
                {
                    CustomerGUID = customerGUID,
                    CharacterName = characterName?.Trim() ?? "",
                    Event = eventName?.Trim() ?? "",
                    BeforeID = beforeID,
                    Limit = Math.Clamp(limit, 1, 500)
                });
        }

        public async Task SetPositions(Guid customerGUID, int mapInstanceID, string mapName, IReadOnlyCollection<LivePositionIn> positions)
        {
            List<LivePositionIn> latest = (positions ?? Array.Empty<LivePositionIn>())
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.CharacterName))
                .GroupBy(p => Truncate(p.CharacterName.Trim(), 50), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Last())
                .Take(500)
                .ToList();
            string[] names = latest.Select(p => Truncate(p.CharacterName.Trim(), 50)).ToArray();

            await using DbConnection connection = CreateConnection();
            await connection.ExecuteAsync(@"DELETE FROM LivePositions
                WHERE CustomerGUID = @CustomerGUID AND MapInstanceID = @MapInstanceID AND NOT (CharacterName = ANY(@Names))",
                new { CustomerGUID = customerGUID, MapInstanceID = mapInstanceID, Names = names });
            if (latest.Count == 0) return;

            await connection.ExecuteAsync(@"INSERT INTO LivePositions (CustomerGUID, CharacterName, MapInstanceID, MapName, X, Y, Z, UpdatedAt)
                SELECT @CustomerGUID, P.Name, @MapInstanceID, @MapName, P.X, P.Y, P.Z, NOW()
                FROM UNNEST(@Names, @Xs, @Ys, @Zs) AS P(Name, X, Y, Z)
                ON CONFLICT (CustomerGUID, CharacterName) DO UPDATE SET MapInstanceID = EXCLUDED.MapInstanceID, MapName = EXCLUDED.MapName,
                    X = EXCLUDED.X, Y = EXCLUDED.Y, Z = EXCLUDED.Z, UpdatedAt = NOW()",
                new
                {
                    CustomerGUID = customerGUID,
                    MapInstanceID = mapInstanceID,
                    MapName = Truncate(mapName, 50),
                    Names = names,
                    Xs = latest.Select(p => p.X).ToArray(),
                    Ys = latest.Select(p => p.Y).ToArray(),
                    Zs = latest.Select(p => p.Z).ToArray()
                });
        }

        public async Task<IEnumerable<LivePosition>> GetPositions(Guid customerGUID, string mapName)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<LivePosition>(@"SELECT LP.CharacterName, LP.MapInstanceID, LP.MapName, LP.X, LP.Y, LP.Z, LP.UpdatedAt,
                    COALESCE(S.Value, 0) AS Level
                FROM LivePositions LP
                LEFT JOIN Characters C ON C.CustomerGUID = LP.CustomerGUID AND C.CharName = LP.CharacterName
                LEFT JOIN CharStats S ON S.CustomerGUID = C.CustomerGUID AND S.CharacterID = C.CharacterID AND S.StatIdentifier = 'BaseLevel'
                WHERE LP.CustomerGUID = @CustomerGUID AND LP.UpdatedAt > NOW() - INTERVAL '30 seconds'
                  AND (@MapName = '' OR LP.MapName = @MapName)
                ORDER BY LP.CharacterName",
                new { CustomerGUID = customerGUID, MapName = mapName?.Trim() ?? "" });
        }

        private static string Truncate(string value, int maxLength)
        {
            return value == null || value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
