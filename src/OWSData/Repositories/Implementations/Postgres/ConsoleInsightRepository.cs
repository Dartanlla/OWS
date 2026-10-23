using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class ConsoleInsightRepository : IConsoleInsightRepository
    {
        private const string PieEmailPattern = "%@samsara.local";

        private const string PieNames = @"Pie AS (SELECT C.CharName FROM Characters C
                INNER JOIN Users U ON U.CustomerGUID = C.CustomerGUID AND U.UserGUID = C.UserGUID
                WHERE C.CustomerGUID = @CustomerGUID AND U.Email ILIKE @PieEmail)";

        private readonly IOptions<StorageOptions> _storageOptions;

        public ConsoleInsightRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        private async Task<IEnumerable<T>> Query<T>(string sql, Guid customerGUID, object extra = null)
        {
            DynamicParameters parameters = new(extra);
            parameters.Add("CustomerGUID", customerGUID);
            parameters.Add("PieEmail", PieEmailPattern);
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<T>(sql, parameters, commandTimeout: 30);
        }

        public Task<IEnumerable<NewPlayerCohort>> GetNewPlayers(Guid customerGUID)
        {
            return Query<NewPlayerCohort>(@"WITH Accounts AS (
                    SELECT U.UserGUID, (U.CreateDate AT TIME ZONE 'UTC')::DATE AS Day FROM Users U
                    WHERE U.CustomerGUID = @CustomerGUID AND U.CreateDate > NOW() - INTERVAL '30 days' AND U.Email NOT ILIKE @PieEmail),
                AccountCharacters AS (
                    SELECT C.UserGUID, C.CharacterID, C.CharName FROM Characters C
                    INNER JOIN Accounts A ON A.UserGUID = C.UserGUID WHERE C.CustomerGUID = @CustomerGUID),
                Levels AS (
                    SELECT AC.UserGUID, MAX(S.Value) AS Level FROM AccountCharacters AC
                    INNER JOIN CharStats S ON S.CustomerGUID = @CustomerGUID AND S.CharacterID = AC.CharacterID AND S.StatIdentifier = 'BaseLevel'
                    GROUP BY AC.UserGUID),
                Played AS (
                    SELECT DISTINCT AC.UserGUID, (E.EventTime AT TIME ZONE 'UTC')::DATE AS Day FROM AccountCharacters AC
                    INNER JOIN GameEvents E ON E.CustomerGUID = @CustomerGUID AND E.CharacterName = AC.CharName AND E.Event = 'session_started')
                SELECT A.Day, COUNT(*)::INT AS Accounts,
                    COUNT(*) FILTER (WHERE EXISTS (SELECT 1 FROM AccountCharacters AC WHERE AC.UserGUID = A.UserGUID))::INT AS MadeCharacter,
                    COUNT(*) FILTER (WHERE L.Level >= 5)::INT AS Level5,
                    COUNT(*) FILTER (WHERE L.Level >= 10)::INT AS Level10,
                    COUNT(*) FILTER (WHERE EXISTS (SELECT 1 FROM Played P WHERE P.UserGUID = A.UserGUID AND P.Day = A.Day + 1))::INT AS NextDay,
                    COUNT(*) FILTER (WHERE EXISTS (SELECT 1 FROM Played P WHERE P.UserGUID = A.UserGUID AND P.Day >= A.Day + 7))::INT AS AfterAWeek
                FROM Accounts A LEFT JOIN Levels L ON L.UserGUID = A.UserGUID
                GROUP BY A.Day ORDER BY A.Day DESC", customerGUID);
        }

        public Task<IEnumerable<StoppedPlayer>> GetStoppedPlayers(Guid customerGUID)
        {
            return Query<StoppedPlayer>(@"WITH Recent AS (
                    SELECT C.CharacterID, C.CharName, C.CreateDate, C.MapName FROM Characters C
                    INNER JOIN Users U ON U.CustomerGUID = C.CustomerGUID AND U.UserGUID = C.UserGUID
                    WHERE C.CustomerGUID = @CustomerGUID AND C.CreateDate > NOW() - INTERVAL '30 days' AND U.Email NOT ILIKE @PieEmail),
                Sessions AS (
                    SELECT E.CharacterName, E.EventTime, E.MapName, E.Fields FROM GameEvents E INNER JOIN Recent R ON R.CharName = E.CharacterName
                    WHERE E.CustomerGUID = @CustomerGUID AND E.Event = 'session_ended'),
                LastLeave AS (
                    SELECT DISTINCT ON (CharacterName) CharacterName, EventTime, MapName FROM Sessions
                    WHERE COALESCE(Fields ->> 'reason', '') <> 'zone_travel' ORDER BY CharacterName, EventTime DESC),
                LastStart AS (
                    SELECT E.CharacterName, MAX(E.EventTime) AS EventTime FROM GameEvents E INNER JOIN Recent R ON R.CharName = E.CharacterName
                    WHERE E.CustomerGUID = @CustomerGUID AND E.Event = 'session_started' GROUP BY E.CharacterName),
                Played AS (
                    SELECT CharacterName, SUM(NULLIF(Fields ->> 'seconds', '')::NUMERIC) / 60.0 AS Minutes FROM Sessions GROUP BY CharacterName),
                LastQuest AS (
                    SELECT DISTINCT ON (E.CharacterName) E.CharacterName, E.Fields ->> 'quest' AS Quest, E.Event FROM GameEvents E
                    INNER JOIN Recent R ON R.CharName = E.CharacterName
                    WHERE E.CustomerGUID = @CustomerGUID AND E.Event IN ('quest_accepted', 'quest_completed')
                    ORDER BY E.CharacterName, E.EventTime DESC)
                SELECT R.CharName AS CharacterName, R.CreateDate, COALESCE(S.Value, 1) AS Level, COALESCE(L.MapName, R.MapName) AS MapName,
                    L.EventTime AS LastSeen, Q.Quest AS LastQuest, Q.Event AS LastQuestEvent, COALESCE(P.Minutes, 0)::FLOAT8 AS MinutesPlayed
                FROM Recent R
                LEFT JOIN LastLeave L ON L.CharacterName = R.CharName
                LEFT JOIN Played P ON P.CharacterName = R.CharName
                LEFT JOIN LastQuest Q ON Q.CharacterName = R.CharName
                LEFT JOIN LastStart LS ON LS.CharacterName = R.CharName
                LEFT JOIN CharStats S ON S.CustomerGUID = @CustomerGUID AND S.CharacterID = R.CharacterID AND S.StatIdentifier = 'BaseLevel'
                -- A session that never logged its end (a zone crash) still counts as play.
                WHERE COALESCE(GREATEST(L.EventTime, LS.EventTime), R.CreateDate) < NOW() - INTERVAL '3 days'
                  AND NOT EXISTS (SELECT 1 FROM CharOnMapInstance CMI WHERE CMI.CustomerGUID = @CustomerGUID AND CMI.CharacterID = R.CharacterID)
                ORDER BY L.EventTime DESC NULLS LAST LIMIT 300", customerGUID);
        }

        public Task<IEnumerable<QuestFunnelRow>> GetQuestFunnel(Guid customerGUID)
        {
            return Query<QuestFunnelRow>($@"WITH {PieNames},
                Pairs AS (
                    SELECT CharacterName, Fields ->> 'quest' AS Quest,
                        MIN(EventTime) FILTER (WHERE Event = 'quest_accepted') AS AcceptedAt,
                        MIN(EventTime) FILTER (WHERE Event = 'quest_completed') AS CompletedAt
                    FROM GameEvents
                    WHERE CustomerGUID = @CustomerGUID AND Event IN ('quest_accepted', 'quest_completed') AND EventTime > NOW() - INTERVAL '30 days'
                      AND CharacterName NOT IN (SELECT CharName FROM Pie)
                    GROUP BY CharacterName, Fields ->> 'quest')
                SELECT Quest, COUNT(AcceptedAt)::INT AS Accepted, COUNT(CompletedAt) FILTER (WHERE AcceptedAt IS NOT NULL)::INT AS Completed,
                    (PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY EXTRACT(EPOCH FROM CompletedAt - AcceptedAt) / 60.0)
                        FILTER (WHERE AcceptedAt IS NOT NULL AND CompletedAt >= AcceptedAt))::FLOAT8 AS MedianMinutes
                FROM Pairs WHERE Quest IS NOT NULL AND AcceptedAt IS NOT NULL
                GROUP BY Quest ORDER BY COUNT(AcceptedAt) DESC LIMIT 300", customerGUID);
        }

        public Task<IEnumerable<OutlierRow>> GetOutliers(Guid customerGUID)
        {
            return Query<OutlierRow>($@"WITH {PieNames},
                Events AS (
                    SELECT CharacterName, Event, Fields FROM GameEvents
                    WHERE CustomerGUID = @CustomerGUID AND EventTime > NOW() - INTERVAL '7 days' AND CharacterName IS NOT NULL
                      AND Event IN ('session_ended', 'currency_changed', 'level_up', 'item_gained')
                      AND CharacterName NOT IN (SELECT CharName FROM Pie)),
                Totals AS (
                    SELECT CharacterName,
                        SUM(CASE WHEN Event = 'session_ended' THEN NULLIF(Fields ->> 'seconds', '')::NUMERIC END) / 3600.0 AS Hours,
                        MAX(CASE WHEN Event = 'session_ended' THEN NULLIF(Fields ->> 'seconds', '')::NUMERIC END) / 3600.0 AS Longest,
                        SUM(CASE WHEN Event = 'currency_changed' AND NULLIF(Fields ->> 'delta', '')::NUMERIC > 0
                                  AND COALESCE(Fields ->> 'source', '') NOT IN ('trade', 'staff')
                                 THEN NULLIF(Fields ->> 'delta', '')::NUMERIC ELSE 0 END) AS Juri,
                        SUM(CASE WHEN Event = 'level_up' AND Fields ->> 'kind' = 'base'
                                 THEN COALESCE(NULLIF(Fields ->> 'gained', '')::NUMERIC, 1) ELSE 0 END) AS Levels,
                        SUM(CASE WHEN Event = 'item_gained' AND Fields ->> 'source' = 'loot'
                                 THEN COALESCE(NULLIF(Fields ->> 'qty', '')::NUMERIC, 1) ELSE 0 END) AS Loot
                    FROM Events GROUP BY CharacterName),
                Rates AS (
                    SELECT CharacterName, Hours, Longest, Juri / Hours AS JuriPerHour, Levels / Hours AS LevelsPerHour, Loot / Hours AS LootPerHour
                    FROM Totals WHERE Hours >= 1),
                Medians AS (
                    SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY JuriPerHour) AS J,
                        PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY LevelsPerHour) AS L,
                        PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY LootPerHour) AS I,
                        PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY Longest) AS S
                    FROM Rates)
                SELECT R.CharacterName, R.Hours::FLOAT8 AS Hours, R.Longest::FLOAT8 AS LongestSessionHours, R.JuriPerHour::FLOAT8 AS JuriPerHour,
                    R.LevelsPerHour::FLOAT8 AS LevelsPerHour, R.LootPerHour::FLOAT8 AS LootPerHour,
                    M.J::FLOAT8 AS MedianJuriPerHour, M.L::FLOAT8 AS MedianLevelsPerHour, M.I::FLOAT8 AS MedianLootPerHour,
                    M.S::FLOAT8 AS MedianLongestSessionHours
                FROM Rates R CROSS JOIN Medians M
                WHERE (M.J > 0 AND R.JuriPerHour > 3 * M.J) OR (M.L > 0 AND R.LevelsPerHour > 3 * M.L)
                   OR (M.I > 0 AND R.LootPerHour > 3 * M.I) OR (M.S > 0 AND R.Longest > 3 * M.S)
                ORDER BY GREATEST(R.JuriPerHour / NULLIF(M.J, 0), R.LevelsPerHour / NULLIF(M.L, 0), R.LootPerHour / NULLIF(M.I, 0),
                                  R.Longest / NULLIF(M.S, 0)) DESC NULLS LAST
                LIMIT 100", customerGUID);
        }

        public Task<IEnumerable<TradePartnerRow>> GetTradePartners(Guid customerGUID, string characterName)
        {
            return Query<TradePartnerRow>(@"SELECT Fields ->> 'partner' AS Partner, COUNT(*)::INT AS Trades,
                    COALESCE(SUM(NULLIF(Fields ->> 'juriGiven', '')::BIGINT), 0)::BIGINT AS JuriGiven,
                    COALESCE(SUM(NULLIF(Fields ->> 'juriReceived', '')::BIGINT), 0)::BIGINT AS JuriReceived,
                    COALESCE(SUM(JSONB_ARRAY_LENGTH(COALESCE(Fields -> 'itemsGiven', '[]'::JSONB))), 0)::INT AS ItemsGiven,
                    COALESCE(SUM(JSONB_ARRAY_LENGTH(COALESCE(Fields -> 'itemsReceived', '[]'::JSONB))), 0)::INT AS ItemsReceived,
                    MAX(EventTime) AS LastTrade
                FROM GameEvents
                WHERE CustomerGUID = @CustomerGUID AND CharacterName = @Name AND Event = 'trade'
                GROUP BY Fields ->> 'partner' ORDER BY MAX(EventTime) DESC LIMIT 50",
                customerGUID, new { Name = characterName?.Trim() ?? "" });
        }

        public Task<IEnumerable<TradePairRow>> GetTradePairs(Guid customerGUID)
        {
            return Query<TradePairRow>($@"WITH {PieNames}
                SELECT CharacterName AS A, Fields ->> 'partner' AS B, COUNT(*)::INT AS Trades,
                    COALESCE(SUM(NULLIF(Fields ->> 'juriGiven', '')::BIGINT), 0)::BIGINT AS AGaveJuri,
                    COALESCE(SUM(NULLIF(Fields ->> 'juriReceived', '')::BIGINT), 0)::BIGINT AS BGaveJuri,
                    COALESCE(SUM(JSONB_ARRAY_LENGTH(COALESCE(Fields -> 'itemsGiven', '[]'::JSONB))), 0)::INT AS AGaveItems,
                    COALESCE(SUM(JSONB_ARRAY_LENGTH(COALESCE(Fields -> 'itemsReceived', '[]'::JSONB))), 0)::INT AS BGaveItems
                FROM GameEvents
                WHERE CustomerGUID = @CustomerGUID AND Event = 'trade' AND EventTime > NOW() - INTERVAL '7 days'
                  AND CharacterName < Fields ->> 'partner'
                  AND CharacterName NOT IN (SELECT CharName FROM Pie)
                GROUP BY CharacterName, Fields ->> 'partner'
                ORDER BY SUM(COALESCE(NULLIF(Fields ->> 'juriGiven', '')::BIGINT, 0) + COALESCE(NULLIF(Fields ->> 'juriReceived', '')::BIGINT, 0)) DESC,
                    COUNT(*) DESC
                LIMIT 100", customerGUID);
        }

        public Task<IEnumerable<DropMobRow>> GetDropMobs(Guid customerGUID)
        {
            return Query<DropMobRow>(@"SELECT Fields ->> 'mob' AS Mob, COALESCE(SUM(NULLIF(Fields ->> 'kills', '')::BIGINT), 0)::BIGINT AS Kills
                FROM GameEvents
                WHERE CustomerGUID = @CustomerGUID AND Event = 'loot_stats' AND EventTime > NOW() - INTERVAL '7 days'
                GROUP BY Fields ->> 'mob' ORDER BY SUM(NULLIF(Fields ->> 'kills', '')::BIGINT) DESC NULLS LAST LIMIT 300", customerGUID);
        }

        public Task<IEnumerable<DropItemRow>> GetDropItems(Guid customerGUID)
        {
            return Query<DropItemRow>(@"WITH Rolls AS (
                    SELECT E.Fields ->> 'mob' AS Mob, R ->> 'item' AS Item,
                        COALESCE(NULLIF(R ->> 'rolls', '')::BIGINT, 0) AS Rolls,
                        COALESCE(NULLIF(R ->> 'drops', '')::BIGINT, 0) AS Drops,
                        COALESCE(NULLIF(R ->> 'chance', '')::FLOAT8, 0) AS Chance,
                        LEAST(1, COALESCE(NULLIF(R ->> 'chance', '')::FLOAT8, 0) * COALESCE(NULLIF(R ->> 'multiplier', '')::FLOAT8, 1)) AS P
                    FROM GameEvents E CROSS JOIN LATERAL JSONB_ARRAY_ELEMENTS(COALESCE(E.Fields -> 'items', '[]'::JSONB)) R
                    WHERE E.CustomerGUID = @CustomerGUID AND E.Event = 'loot_stats' AND E.EventTime > NOW() - INTERVAL '7 days')
                SELECT Mob, Item, SUM(Rolls)::BIGINT AS Rolls, SUM(Drops)::BIGINT AS Drops,
                    COALESCE(SUM(Chance * Rolls) / NULLIF(SUM(Rolls), 0), 0)::FLOAT8 AS Chance,
                    SUM(P * Rolls)::FLOAT8 AS ExpectedDrops,
                    SUM(P * (1 - P) * Rolls)::FLOAT8 AS Variance
                FROM Rolls GROUP BY Mob, Item ORDER BY Mob, Item LIMIT 2000", customerGUID);
        }
    }
}
