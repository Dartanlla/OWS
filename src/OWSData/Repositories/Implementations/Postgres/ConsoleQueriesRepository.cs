using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class ConsoleQueriesRepository : IConsoleQueriesRepository
    {
        // The editor's throwaway PIE accounts (PurgePIEAccounts) are noise in every list unless the search asks for them.
        private const string PieEmailPattern = "%@samsara.local";

        private const string CharacterRowSelect = @"SELECT C.CharacterID, C.CharName, C.UserGUID, U.FirstName AS AccountName,
                SamsaraTryJsonb((SELECT D.FieldValue FROM CustomCharacterData D
                    WHERE D.CustomerGUID = C.CustomerGUID AND D.CharacterID = C.CharacterID AND D.CustomFieldName = 'CharacterPersistentData'
                    ORDER BY D.CustomCharacterDataID DESC LIMIT 1)) ->> 'classTag' AS ClassTag,
                (SELECT S.Value FROM CharStats S WHERE S.CustomerGUID = C.CustomerGUID AND S.CharacterID = C.CharacterID AND S.StatIdentifier = 'BaseLevel') AS BaseLevel,
                (SELECT S.Value FROM CharStats S WHERE S.CustomerGUID = C.CustomerGUID AND S.CharacterID = C.CharacterID AND S.StatIdentifier = 'BaseJobLevel') AS JobLevel,
                C.MapName, C.Gold, C.LastActivity, C.CreateDate,
                O.MapInstanceID AS OnlineMapInstanceID, O.ZoneName AS OnlineZone";

        private const string CharacterRowFrom = @"FROM Characters C
                LEFT JOIN Users U ON U.UserGUID = C.UserGUID
                LEFT JOIN LATERAL (SELECT CMI.MapInstanceID, M.ZoneName FROM CharOnMapInstance CMI
                    INNER JOIN MapInstances MI ON MI.CustomerGUID = CMI.CustomerGUID AND MI.MapInstanceID = CMI.MapInstanceID
                    INNER JOIN Maps M ON M.CustomerGUID = MI.CustomerGUID AND M.MapID = MI.MapID
                    WHERE CMI.CustomerGUID = C.CustomerGUID AND CMI.CharacterID = C.CharacterID LIMIT 1) O ON TRUE";

        private const string ActiveSanctionExists = @"EXISTS (SELECT 1 FROM AccountSanctions S WHERE S.CustomerGUID = U.CustomerGUID AND S.UserGUID = U.UserGUID
                AND S.Kind = '{0}' AND S.RevokedAt IS NULL AND (S.ExpiresAt IS NULL OR S.ExpiresAt > NOW()))";

        private const string ShopSelect = @"SELECT S.PlayerShopID, S.OwnerCharacterID, S.OwnerCharName, S.ShopName, S.ZoneName, S.SaleStatus, S.SettlementStatus,
                S.ProceedsGoldPending, S.CreatedAt, S.ClosedAt,
                (SELECT COUNT(*) FROM PlayerShopItems I WHERE I.PlayerShopID = S.PlayerShopID AND I.QuantityRemainingForSale > 0) AS Listings
                FROM PlayerShops S";

        private const string AuditColumns = "AuditLogID, CreateDate, Source, Actor, Action, Target, Detail, ClientIP";

        private readonly IOptions<StorageOptions> _storageOptions;

        public ConsoleQueriesRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        /// <summary>A juri change at least this large shows as an Overview signal.</summary>
        public static long LargeJuriChange { get; set; } = 100000;

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        private static string LikePattern(string search)
        {
            return "%" + (search ?? "").Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        }

        public async Task<ConsoleOverview> GetOverview(Guid customerGUID)
        {
            await using DbConnection connection = CreateConnection();
            var parameters = new { CustomerGUID = customerGUID, LargeJuriChange };

            ConsoleOverview overview = await connection.QueryFirstAsync<ConsoleOverview>(@"SELECT
                (SELECT COUNT(*) FROM CharOnMapInstance WHERE CustomerGUID = @CustomerGUID)::INT AS OnlineNow,
                (SELECT COUNT(*) FROM MapInstances WHERE CustomerGUID = @CustomerGUID AND Status = 2)::INT AS ZonesUp,
                (SELECT COUNT(DISTINCT CASE WHEN Target = 'password' THEN LOWER(Actor) ELSE Target END) FROM AuditLog
                    WHERE CustomerGUID = @CustomerGUID AND Action = 'user_login' AND CreateDate >= date_trunc('day', NOW())
                      AND SamsaraTryJsonb(Detail) ->> 'result' = 'ok')::INT AS AccountsToday,
                (SELECT COUNT(*) FROM Characters WHERE CustomerGUID = @CustomerGUID AND CreateDate >= date_trunc('day', NOW()))::INT AS NewCharactersToday,
                (SELECT percentile_cont(0.5) WITHIN GROUP (ORDER BY (Fields ->> 'seconds')::NUMERIC) / 60.0 FROM GameEvents
                    WHERE CustomerGUID = @CustomerGUID AND Event = 'session_ended' AND EventTime > NOW() - INTERVAL '7 days'
                      AND (Fields ->> 'seconds') IS NOT NULL)::FLOAT8 AS MedianSessionMinutes7d", parameters);

            overview.Signals = (await connection.QueryAsync<ConsoleSignal>(@"
                (SELECT 'save_failed' AS Kind, CharacterName AS Subject, string_agg(DISTINCT Fields ->> 'endpoint', ', ') AS Detail,
                    COUNT(*) AS Count, MAX(EventTime) AS LastSeen
                 FROM GameEvents WHERE CustomerGUID = @CustomerGUID AND Event = 'persistence_failed' AND EventTime > NOW() - INTERVAL '24 hours'
                 GROUP BY CharacterName ORDER BY COUNT(*) DESC LIMIT 10)
                UNION ALL
                (SELECT 'rejected', CharacterName, string_agg(DISTINCT Fields ->> 'rpc', ', '), COUNT(*), MAX(EventTime)
                 FROM GameEvents WHERE CustomerGUID = @CustomerGUID AND Event = 'rpc_rejected' AND EventTime > NOW() - INTERVAL '24 hours'
                 GROUP BY CharacterName ORDER BY COUNT(*) DESC LIMIT 10)
                UNION ALL
                (SELECT 'juri', CharacterName, COALESCE(Fields ->> 'source', 'other') || ' ' || (Fields ->> 'delta'), 1, EventTime
                 FROM GameEvents WHERE CustomerGUID = @CustomerGUID AND Event = 'currency_changed' AND EventTime > NOW() - INTERVAL '24 hours'
                   AND ABS((Fields ->> 'delta')::NUMERIC) >= @LargeJuriChange
                 ORDER BY ABS((Fields ->> 'delta')::NUMERIC) DESC LIMIT 10)
                UNION ALL
                (SELECT 'zone_exit', Target, 'exit code ' || COALESCE(SamsaraTryJsonb(Detail) ->> 'exitCode', '?'), COUNT(*), MAX(CreateDate)
                 FROM AuditLog WHERE CustomerGUID = @CustomerGUID AND Action = 'zone_exit' AND CreateDate > NOW() - INTERVAL '24 hours'
                 GROUP BY Target, SamsaraTryJsonb(Detail) ->> 'exitCode' ORDER BY COUNT(*) DESC LIMIT 10)
                UNION ALL
                (SELECT 'duplicate', 'items', 'instance IDs held more than once', COUNT(*), NULL FROM (
                    SELECT CII.InstanceId FROM CharInventoryItems CII
                    WHERE CII.CustomerGUID = @CustomerGUID AND CII.InstanceId IS NOT NULL AND CII.Quantity = 1
                    GROUP BY CII.InstanceId HAVING COUNT(*) > 1) D HAVING COUNT(*) > 0)", parameters)).ToList();

            return overview;
        }

        public async Task<IEnumerable<ConsoleAccountRow>> SearchAccounts(Guid customerGUID, string search, int limit)
        {
            search = (search ?? "").Trim();
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<ConsoleAccountRow>($@"SELECT U.UserGUID, U.FirstName, U.Email, U.SteamId, U.Role, U.PreferredServerRegion,
                    U.CreateDate, U.LastAccess,
                    (SELECT COUNT(*) FROM Characters C WHERE C.CustomerGUID = U.CustomerGUID AND C.UserGUID = U.UserGUID)::INT AS CharacterCount,
                    {string.Format(ActiveSanctionExists, SanctionKinds.Ban)} AS Banned,
                    {string.Format(ActiveSanctionExists, SanctionKinds.Mute)} AS Muted
                FROM Users U
                WHERE U.CustomerGUID = @CustomerGUID
                  AND (@Search = '' OR U.Email ILIKE @Pattern OR U.FirstName ILIKE @Pattern OR U.SteamId = @Search
                       OR CAST(U.UserGUID AS TEXT) = LOWER(@Search))
                  AND (U.Email NOT ILIKE @PieEmail OR @Search ILIKE '%samsara.local%')
                ORDER BY U.LastAccess DESC LIMIT @Limit",
                new { CustomerGUID = customerGUID, Search = search, Pattern = LikePattern(search), PieEmail = PieEmailPattern, Limit = Math.Clamp(limit, 1, 200) });
        }

        public async Task<IEnumerable<ConsoleCharacterRow>> SearchCharacters(Guid customerGUID, string search, int limit)
        {
            search = (search ?? "").Trim();
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<ConsoleCharacterRow>($@"{CharacterRowSelect} {CharacterRowFrom}
                WHERE C.CustomerGUID = @CustomerGUID
                  AND (@Search = '' OR C.CharName ILIKE @Pattern)
                  AND (U.Email IS NULL OR U.Email NOT ILIKE @PieEmail OR @Search ILIKE '%samsara.local%')
                ORDER BY (O.MapInstanceID IS NULL), C.LastActivity DESC LIMIT @Limit",
                new { CustomerGUID = customerGUID, Search = search, Pattern = LikePattern(search), PieEmail = PieEmailPattern, Limit = Math.Clamp(limit, 1, 200) });
        }

        public async Task<ConsoleAccountRow> GetAccount(Guid customerGUID, Guid userGUID)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<ConsoleAccountRow>($@"SELECT U.UserGUID, U.FirstName, U.Email, U.SteamId, U.Role,
                    U.PreferredServerRegion, U.CreateDate, U.LastAccess, U.IsInternalNetworkTestUser,
                    (SELECT COUNT(*) FROM Characters C WHERE C.CustomerGUID = U.CustomerGUID AND C.UserGUID = U.UserGUID)::INT AS CharacterCount,
                    {string.Format(ActiveSanctionExists, SanctionKinds.Ban)} AS Banned,
                    {string.Format(ActiveSanctionExists, SanctionKinds.Mute)} AS Muted
                FROM Users U WHERE U.CustomerGUID = @CustomerGUID AND U.UserGUID = @UserGUID",
                new { CustomerGUID = customerGUID, UserGUID = userGUID });
        }

        public async Task<IEnumerable<ConsoleCharacterRow>> GetCharactersForAccount(Guid customerGUID, Guid userGUID)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<ConsoleCharacterRow>($@"{CharacterRowSelect} {CharacterRowFrom}
                WHERE C.CustomerGUID = @CustomerGUID AND C.UserGUID = @UserGUID
                ORDER BY C.LastActivity DESC", new { CustomerGUID = customerGUID, UserGUID = userGUID });
        }

        public async Task<IEnumerable<AuditEntry>> GetLoginsForAccount(Guid customerGUID, Guid userGUID, int limit)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<AuditEntry>($@"SELECT A.{AuditColumns.Replace(", ", ", A.")} FROM AuditLog A
                INNER JOIN Users U ON U.UserGUID = @UserGUID AND U.CustomerGUID = A.CustomerGUID
                WHERE A.CustomerGUID = @CustomerGUID AND A.Action = 'user_login'
                  AND (A.Target = 'steam:' || U.SteamId OR (A.Target = 'password' AND LOWER(A.Actor) = LOWER(U.Email)))
                ORDER BY A.CreateDate DESC LIMIT @Limit",
                new { CustomerGUID = customerGUID, UserGUID = userGUID, Limit = Math.Clamp(limit, 1, 500) });
        }

        public async Task<ConsoleCharacterDetail> GetCharacter(Guid customerGUID, string characterName)
        {
            await using DbConnection connection = CreateConnection();
            ConsoleCharacterDetail detail = await connection.QueryFirstOrDefaultAsync<ConsoleCharacterDetail>($@"{CharacterRowSelect},
                    C.X, C.Y, C.Z, C.Gender, C.IsAdmin, C.IsModerator, C.IsInternalNetworkTestUser,
                    COALESCE(U.IsInternalNetworkTestUser, FALSE) AS AccountIsInternalNetworkTestUser, C.EconomyRevision, U.Role
                {CharacterRowFrom}
                WHERE C.CustomerGUID = @CustomerGUID AND LOWER(C.CharName) = LOWER(@CharName)
                LIMIT 1", new { CustomerGUID = customerGUID, CharName = characterName?.Trim() ?? "" });

            if (detail == null) return null;

            var byId = new { CustomerGUID = customerGUID, detail.CharacterID };

            detail.Stats = (await connection.QueryAsync<(string StatIdentifier, int Value)>(
                    "SELECT StatIdentifier, Value FROM CharStats WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID ORDER BY StatIdentifier", byId))
                .ToDictionary(stat => stat.StatIdentifier, stat => stat.Value);

            detail.Items = (await connection.QueryAsync<ConsoleItem>(@"SELECT CII.InSlotNumber, CII.ItemIDTag, CII.Quantity, CII.InstanceId, CII.CustomData
                    FROM CharInventoryItems CII
                    INNER JOIN CharInventory CI ON CI.CustomerGUID = CII.CustomerGUID AND CI.CharInventoryID = CII.CharInventoryID
                    WHERE CII.CustomerGUID = @CustomerGUID AND CI.CharacterID = @CharacterID
                    ORDER BY CII.InSlotNumber", byId)).ToList();
            foreach (ConsoleItem item in detail.Items)
            {
                DecodeItemState(item);
            }

            detail.Quests = (await connection.QueryAsync<ConsoleQuest>(
                "SELECT QuestIDTag, QuestJournalTagContainer FROM CharQuests WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID ORDER BY QuestIDTag",
                byId)).ToList();

            detail.Shops = (await connection.QueryAsync<ConsoleShop>(
                $"{ShopSelect} WHERE S.CustomerGUID = @CustomerGUID AND S.OwnerCharacterID = @CharacterID ORDER BY S.CreatedAt DESC LIMIT 20", byId)).ToList();

            return detail;
        }

        /// <summary>CustomData is the item's persisted state (refinementLevel, starLevel, bLocked, bDamaged, equippedCards).</summary>
        public static void DecodeItemState(ConsoleItem item)
        {
            if (string.IsNullOrWhiteSpace(item.CustomData)) return;

            try
            {
                using JsonDocument document = JsonDocument.Parse(item.CustomData);
                JsonElement root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return;

                item.Refine = ReadInt(root, "refinementLevel");
                item.Stars = ReadInt(root, "starLevel");
                item.Locked = ReadBool(root, "bLocked");
                item.Damaged = ReadBool(root, "bDamaged");
                if (root.TryGetProperty("equippedCards", out JsonElement cards) && cards.ValueKind == JsonValueKind.Array)
                {
                    item.Cards = cards.EnumerateArray()
                        .Where(card => card.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(card.GetString()))
                        .Select(card => card.GetString())
                        .ToList();
                }
            }
            catch (JsonException)
            {
                // Legacy rows hold free text; the raw CustomData still goes to the console.
            }
        }

        private static int ReadInt(JsonElement root, string name)
        {
            return root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) ? number : 0;
        }

        private static bool ReadBool(JsonElement root, string name)
        {
            return root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.True;
        }

        public async Task<IEnumerable<AuditEntry>> GetAuditForCharacter(Guid customerGUID, string characterName, int limit)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<AuditEntry>($@"SELECT {AuditColumns} FROM AuditLog
                WHERE CustomerGUID = @CustomerGUID AND (LOWER(Actor) = LOWER(@Name) OR LOWER(Target) = LOWER(@Name))
                ORDER BY CreateDate DESC, AuditLogID DESC LIMIT @Limit",
                new { CustomerGUID = customerGUID, Name = characterName?.Trim() ?? "", Limit = Math.Clamp(limit, 1, 500) });
        }

        public async Task<ConsoleDistribution> GetDistribution(Guid customerGUID)
        {
            await using DbConnection connection = CreateConnection();
            var parameters = new { CustomerGUID = customerGUID, PieEmail = PieEmailPattern };
            return new ConsoleDistribution
            {
                Levels = (await connection.QueryAsync<ConsoleCount>(@"SELECT S.Value::TEXT AS Key, COUNT(*) AS Count
                    FROM CharStats S
                    INNER JOIN Characters C ON C.CustomerGUID = S.CustomerGUID AND C.CharacterID = S.CharacterID
                    LEFT JOIN Users U ON U.UserGUID = C.UserGUID
                    WHERE S.CustomerGUID = @CustomerGUID AND S.StatIdentifier = 'BaseLevel' AND (U.Email IS NULL OR U.Email NOT ILIKE @PieEmail)
                    GROUP BY S.Value ORDER BY S.Value", parameters)).ToList(),
                Classes = (await connection.QueryAsync<ConsoleCount>(@"SELECT COALESCE(SamsaraTryJsonb(D.FieldValue) ->> 'classTag', 'unknown') AS Key, COUNT(DISTINCT D.CharacterID) AS Count
                    FROM CustomCharacterData D
                    INNER JOIN Characters C ON C.CustomerGUID = D.CustomerGUID AND C.CharacterID = D.CharacterID
                    LEFT JOIN Users U ON U.UserGUID = C.UserGUID
                    WHERE D.CustomerGUID = @CustomerGUID AND D.CustomFieldName = 'CharacterPersistentData'
                      AND (U.Email IS NULL OR U.Email NOT ILIKE @PieEmail)
                    GROUP BY 1 ORDER BY 2 DESC", parameters)).ToList()
            };
        }

        public async Task<ConsoleEconomy> GetEconomy(Guid customerGUID)
        {
            await using DbConnection connection = CreateConnection();
            var parameters = new { CustomerGUID = customerGUID };

            ConsoleEconomy economy = await connection.QueryFirstAsync<ConsoleEconomy>(@"SELECT COALESCE(SUM(Gold), 0)::BIGINT AS JuriTotal,
                    COALESCE(percentile_cont(0.5) WITHIN GROUP (ORDER BY Gold), 0)::BIGINT AS JuriMedian, COUNT(*)::INT AS Characters
                FROM Characters WHERE CustomerGUID = @CustomerGUID", parameters);

            economy.TopHolders = (await connection.QueryAsync<ConsoleCharacterRow>($@"{CharacterRowSelect} {CharacterRowFrom}
                WHERE C.CustomerGUID = @CustomerGUID ORDER BY C.Gold DESC LIMIT 20", parameters)).ToList();

            economy.Flows7d = (await connection.QueryAsync<ConsoleJuriFlow>(@"SELECT COALESCE(NULLIF(Fields ->> 'source', ''), 'other') AS Source,
                    SUM(GREATEST((Fields ->> 'delta')::BIGINT, 0))::BIGINT AS JuriIn,
                    SUM(GREATEST(-(Fields ->> 'delta')::BIGINT, 0))::BIGINT AS JuriOut,
                    COUNT(*) AS Events
                FROM GameEvents
                WHERE CustomerGUID = @CustomerGUID AND Event = 'currency_changed' AND EventTime > NOW() - INTERVAL '7 days'
                GROUP BY 1 ORDER BY SUM(ABS((Fields ->> 'delta')::BIGINT)) DESC", parameters)).ToList();

            economy.Items = (await connection.QueryAsync<ConsoleItemCensus>(@"SELECT CII.ItemIDTag, SUM(CII.Quantity)::BIGINT AS Quantity,
                    COUNT(DISTINCT CI.CharacterID) AS Holders,
                    COALESCE(MAX((SamsaraTryJsonb(CII.CustomData) ->> 'refinementLevel')::INT), 0) AS MaxRefine
                FROM CharInventoryItems CII
                INNER JOIN CharInventory CI ON CI.CustomerGUID = CII.CustomerGUID AND CI.CharInventoryID = CII.CharInventoryID
                WHERE CII.CustomerGUID = @CustomerGUID
                GROUP BY CII.ItemIDTag ORDER BY SUM(CII.Quantity) DESC LIMIT 200", parameters)).ToList();

            economy.Duplicates = (await connection.QueryAsync<ConsoleDuplicate>(@"SELECT CII.InstanceId, MIN(CII.ItemIDTag) AS ItemIDTag, COUNT(*) AS Copies,
                    string_agg(DISTINCT C.CharName, ', ') AS Holders
                FROM CharInventoryItems CII
                INNER JOIN CharInventory CI ON CI.CustomerGUID = CII.CustomerGUID AND CI.CharInventoryID = CII.CharInventoryID
                INNER JOIN Characters C ON C.CustomerGUID = CI.CustomerGUID AND C.CharacterID = CI.CharacterID
                WHERE CII.CustomerGUID = @CustomerGUID AND CII.InstanceId IS NOT NULL AND CII.Quantity = 1
                GROUP BY CII.InstanceId HAVING COUNT(*) > 1
                ORDER BY COUNT(*) DESC LIMIT 100", parameters)).ToList();

            economy.Shops = await connection.QueryFirstAsync<ConsoleShopHealth>(@"SELECT
                (SELECT COUNT(*) FROM PlayerShops WHERE CustomerGUID = @CustomerGUID AND SaleStatus = 'open')::INT AS OpenShops,
                (SELECT COUNT(*) FROM PlayerShopItems I INNER JOIN PlayerShops S ON S.PlayerShopID = I.PlayerShopID
                    WHERE S.CustomerGUID = @CustomerGUID AND S.SaleStatus = 'open' AND I.QuantityRemainingForSale > 0)::INT AS Listings,
                (SELECT COUNT(*) FROM PlayerShopPurchases WHERE CustomerGUID = @CustomerGUID AND State = 'paid'
                    AND CreatedAt < NOW() - INTERVAL '10 minutes')::INT AS UndeliveredPurchases,
                (SELECT COALESCE(SUM(ProceedsGoldPending), 0) FROM PlayerShops WHERE CustomerGUID = @CustomerGUID)::BIGINT AS UnclaimedProceeds,
                (SELECT COUNT(*) FROM PlayerShopOperations WHERE CustomerGUID = @CustomerGUID AND State = 'abandoned'
                    AND CreatedAt > NOW() - INTERVAL '7 days')::INT AS AbandonedOperations7d", parameters);

            economy.Shops.Shops = (await connection.QueryAsync<ConsoleShop>($@"{ShopSelect}
                WHERE S.CustomerGUID = @CustomerGUID AND (S.SaleStatus = 'open' OR S.SettlementStatus <> 'claimed')
                ORDER BY S.CreatedAt DESC LIMIT 100", parameters)).ToList();

            return economy;
        }

        public async Task<IEnumerable<ConsoleMigration>> GetMigrations(int limit)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<ConsoleMigration>(
                "SELECT ScriptName, Applied FROM SchemaVersions ORDER BY SchemaVersionsID DESC LIMIT @Limit", new { Limit = Math.Clamp(limit, 1, 100) });
        }

        public async Task<ConsoleStaffUser> FindUserBySteamId(Guid customerGUID, string steamId)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<ConsoleStaffUser>(
                "SELECT UserGUID, FirstName, Role, SteamId FROM Users WHERE CustomerGUID = @CustomerGUID AND SteamId = @SteamId",
                new { CustomerGUID = customerGUID, SteamId = steamId });
        }

        public async Task<ConsoleStaffUser> GetUser(Guid customerGUID, Guid userGUID)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<ConsoleStaffUser>(
                "SELECT UserGUID, FirstName, Role, SteamId FROM Users WHERE CustomerGUID = @CustomerGUID AND UserGUID = @UserGUID",
                new { CustomerGUID = customerGUID, UserGUID = userGUID });
        }

        public async Task<IEnumerable<string>> GetOnlineCharacterNames(Guid customerGUID, Guid userGUID)
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<string>(@"SELECT DISTINCT C.CharName FROM Characters C
                INNER JOIN CharOnMapInstance CMI ON CMI.CustomerGUID = C.CustomerGUID AND CMI.CharacterID = C.CharacterID
                WHERE C.CustomerGUID = @CustomerGUID AND C.UserGUID = @UserGUID", new { CustomerGUID = customerGUID, UserGUID = userGUID });
        }

        public async Task<IEnumerable<Guid>> GetCustomerGUIDs()
        {
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<Guid>("SELECT CustomerGUID FROM Customers");
        }

        public async Task<IEnumerable<AuditEntry>> GetAudit(Guid customerGUID, string source, string action, string search, int limit)
        {
            search = (search ?? "").Trim();
            await using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<AuditEntry>($@"SELECT {AuditColumns} FROM AuditLog
                WHERE CustomerGUID = @CustomerGUID
                  AND (@Source = '' OR Source = @Source)
                  AND (@Action = '' OR Action = @Action)
                  AND (@Search = '' OR Actor ILIKE @Pattern OR Target ILIKE @Pattern OR Detail ILIKE @Pattern)
                ORDER BY CreateDate DESC, AuditLogID DESC LIMIT @Limit",
                new
                {
                    CustomerGUID = customerGUID,
                    Source = source?.Trim() ?? "",
                    Action = action?.Trim() ?? "",
                    Search = search,
                    Pattern = LikePattern(search),
                    Limit = Math.Clamp(limit, 1, 1000)
                });
        }
    }
}
