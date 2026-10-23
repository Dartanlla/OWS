using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace OWSData.Repositories.Implementations.Postgres
{
    /// <summary>
    /// Restores one character from a backup loaded into a staging database. Snapshot reads either side; Restore copies
    /// the character's rows from staging to live in one transaction: Characters (juri, class and position), CharStats,
    /// CharAbilities, CharQuests, its inventories and items, and CustomCharacterData. Other characters, the account,
    /// shops and escrow are not touched.
    /// </summary>
    public class CharacterRestoreRepository
    {
        public class Snapshot
        {
            public int CharacterID { get; set; }
            public string CharName { get; set; }
            public long Gold { get; set; }
            public int Level { get; set; }
            public int JobLevel { get; set; }
            public string MapName { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public List<SnapshotItem> Items { get; set; } = new();
            public List<SnapshotRow> Abilities { get; set; } = new();
            public List<SnapshotRow> Quests { get; set; } = new();
            /// <summary>Staging only: the backup it was loaded from, or null while a load is not complete.</summary>
            public string Dump { get; set; }
        }

        public class SnapshotItem
        {
            public string Inventory { get; set; }
            public int Slot { get; set; }
            public string Item { get; set; }
            public int Quantity { get; set; }
            public string CustomData { get; set; }
        }

        public class SnapshotRow
        {
            public string Tag { get; set; }
            public string Value { get; set; }
        }

        private static readonly string[] CharacterColumns = { "gold", "classid", "mapname", "x", "y", "z", "rx", "ry", "rz" };

        private readonly string _live;
        private readonly string _staging;

        public CharacterRestoreRepository(string liveConnectionString, string stagingConnectionString)
        {
            _live = PostgresConnectionString.WithPoolDefaults(liveConnectionString);
            _staging = stagingConnectionString;
        }

        /// <summary>Null when the database has no character of that name.</summary>
        public async Task<Snapshot> GetSnapshot(bool fromStaging, Guid customerGUID, string characterName)
        {
            await using DbConnection connection = new NpgsqlConnection(fromStaging ? _staging : _live);
            var args = new { CustomerGUID = customerGUID, Name = characterName?.Trim() ?? "" };
            Snapshot snapshot = await connection.QueryFirstOrDefaultAsync<Snapshot>(@"SELECT C.CharacterID, C.CharName, C.Gold, C.MapName, C.X, C.Y, C.Z,
                    COALESCE((SELECT Value FROM CharStats S WHERE S.CustomerGUID = C.CustomerGUID AND S.CharacterID = C.CharacterID
                              AND S.StatIdentifier = 'BaseLevel'), 1) AS Level,
                    COALESCE((SELECT Value FROM CharStats S WHERE S.CustomerGUID = C.CustomerGUID AND S.CharacterID = C.CharacterID
                              AND S.StatIdentifier = 'BaseJobLevel'), 1) AS JobLevel
                FROM Characters C WHERE C.CustomerGUID = @CustomerGUID AND LOWER(C.CharName) = LOWER(@Name)", args);
            if (snapshot == null) return null;

            var idArgs = new { CustomerGUID = customerGUID, snapshot.CharacterID };
            snapshot.Items = (await connection.QueryAsync<SnapshotItem>(@"SELECT CI.InventoryName AS Inventory, CII.InSlotNumber AS Slot,
                    CII.ItemIDTag AS Item, CII.Quantity, CII.CustomData
                FROM CharInventory CI
                INNER JOIN CharInventoryItems CII ON CII.CustomerGUID = CI.CustomerGUID AND CII.CharInventoryID = CI.CharInventoryID
                WHERE CI.CustomerGUID = @CustomerGUID AND CI.CharacterID = @CharacterID
                ORDER BY CI.InventoryName, CII.InSlotNumber", idArgs)).ToList();
            snapshot.Abilities = (await connection.QueryAsync<SnapshotRow>(@"SELECT AbilityIDTag AS Tag, CurrentAbilityLevel::TEXT AS Value
                FROM CharAbilities WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID ORDER BY AbilityIDTag", idArgs)).ToList();
            snapshot.Quests = (await connection.QueryAsync<SnapshotRow>(@"SELECT QuestIDTag AS Tag, '' AS Value
                FROM CharQuests WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID ORDER BY QuestIDTag", idArgs)).ToList();
            if (fromStaging) snapshot.Dump = await StagingDump(connection, null);
            return snapshot;
        }

        /// <summary>The backup the loader finished loading (staging_meta, written after pg_restore); null while loading.</summary>
        private static async Task<string> StagingDump(DbConnection staging, DbTransaction transaction)
        {
            try
            {
                return await staging.ExecuteScalarAsync<string>("SELECT dump FROM staging_meta LIMIT 1", transaction: transaction);
            }
            catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UndefinedTable)
            {
                return null;
            }
        }

        /// <summary>
        /// Null on success, else why not: "not_in_backup", "not_live", "different_character" (the name now belongs to
        /// another character ID), "online" (in the world, or sent a position in the last minute) or "staging_changed"
        /// (staging no longer holds the backup that was compared, or is still loading).
        /// </summary>
        public async Task<string> Restore(Guid customerGUID, string characterName, string comparedDump)
        {
            string name = characterName?.Trim() ?? "";
            await using NpgsqlConnection staging = new(_staging);
            await staging.OpenAsync();
            // One snapshot of staging, checked to be the backup the caller compared.
            await using NpgsqlTransaction read = await staging.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
            string dump = await StagingDump(staging, read);
            if (dump == null || dump != comparedDump) return "staging_changed";
            int? stagingID = await staging.ExecuteScalarAsync<int?>(
                "SELECT CharacterID FROM Characters WHERE CustomerGUID = @CustomerGUID AND LOWER(CharName) = LOWER(@Name)",
                new { CustomerGUID = customerGUID, Name = name }, read);
            if (!stagingID.HasValue) return "not_in_backup";

            var stagingArgs = new { CustomerGUID = customerGUID, CharacterID = stagingID.Value };
            IDictionary<string, object> character = (IDictionary<string, object>)await staging.QueryFirstAsync(
                "SELECT * FROM Characters WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID", stagingArgs, read);
            List<IDictionary<string, object>> stats = await Rows(staging, read, "CharStats", stagingArgs);
            List<IDictionary<string, object>> abilities = await Rows(staging, read, "CharAbilities", stagingArgs);
            List<IDictionary<string, object>> quests = await Rows(staging, read, "CharQuests", stagingArgs);
            List<IDictionary<string, object>> custom = await Rows(staging, read, "CustomCharacterData", stagingArgs);
            List<IDictionary<string, object>> inventories = await Rows(staging, read, "CharInventory", stagingArgs);
            List<IDictionary<string, object>> items = (await staging.QueryAsync(@"SELECT CII.* FROM CharInventoryItems CII
                INNER JOIN CharInventory CI ON CI.CustomerGUID = CII.CustomerGUID AND CI.CharInventoryID = CII.CharInventoryID
                WHERE CI.CustomerGUID = @CustomerGUID AND CI.CharacterID = @CharacterID", stagingArgs, read))
                .Cast<IDictionary<string, object>>().ToList();

            await using NpgsqlConnection live = new(_live);
            await live.OpenAsync();
            await using NpgsqlTransaction transaction = await live.BeginTransactionAsync();
            var liveArgs = new { CustomerGUID = customerGUID, Name = name };
            await live.ExecuteAsync("SELECT pg_advisory_xact_lock(hashtext('restore:' || LOWER(@Name)))", liveArgs, transaction);

            int? liveID = await live.ExecuteScalarAsync<int?>(
                "SELECT CharacterID FROM Characters WHERE CustomerGUID = @CustomerGUID AND LOWER(CharName) = LOWER(@Name) FOR UPDATE",
                liveArgs, transaction);
            if (!liveID.HasValue) return "not_live";
            if (liveID.Value != stagingID.Value) return "different_character";

            var ids = new { CustomerGUID = customerGUID, CharacterID = liveID.Value, Name = name };
            bool online = await live.ExecuteScalarAsync<bool>(@"SELECT EXISTS (SELECT 1 FROM CharOnMapInstance
                    WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID)
                OR EXISTS (SELECT 1 FROM LivePositions WHERE CustomerGUID = @CustomerGUID AND LOWER(CharacterName) = LOWER(@Name)
                    AND UpdatedAt > NOW() - INTERVAL '1 minute')", ids, transaction);
            if (online) return "online";

            DynamicParameters characterValues = new(ids);
            List<string> sets = new();
            foreach (string column in CharacterColumns.Where(character.ContainsKey))
            {
                characterValues.Add("v_" + column, character[column]);
                sets.Add($"{column} = @v_{column}");
            }
            // A save the old copy still sends carries the old revision and is refused.
            await live.ExecuteAsync($@"UPDATE Characters SET {string.Join(", ", sets)}, EconomyRevision = EconomyRevision + 1
                WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID", characterValues, transaction);

            foreach (string table in new[] { "CharStats", "CharAbilities", "CharQuests", "CustomCharacterData" })
            {
                await live.ExecuteAsync($"DELETE FROM {table} WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID", ids, transaction);
            }
            var keys = new { ids.CustomerGUID, ids.CharacterID };
            await Insert(live, transaction, "CharStats", stats, keys, Array.Empty<string>());
            await Insert(live, transaction, "CharAbilities", abilities, keys, Array.Empty<string>());
            await Insert(live, transaction, "CharQuests", quests, keys, Array.Empty<string>());
            await Insert(live, transaction, "CustomCharacterData", custom, keys, new[] { "customcharacterdataid" });

            await live.ExecuteAsync(@"DELETE FROM CharInventoryItems CII USING CharInventory CI
                WHERE CI.CustomerGUID = CII.CustomerGUID AND CI.CharInventoryID = CII.CharInventoryID
                  AND CI.CustomerGUID = @CustomerGUID AND CI.CharacterID = @CharacterID", ids, transaction);
            foreach (IDictionary<string, object> inventory in inventories)
            {
                var inventoryArgs = new { ids.CustomerGUID, ids.CharacterID, InventoryName = inventory["inventoryname"], InventorySize = inventory["inventorysize"] };
                int liveInventoryID = await live.ExecuteScalarAsync<int?>(@"UPDATE CharInventory SET InventorySize = @InventorySize
                        WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID AND InventoryName = @InventoryName
                        RETURNING CharInventoryID", inventoryArgs, transaction)
                    ?? await live.ExecuteScalarAsync<int>(@"INSERT INTO CharInventory (CustomerGUID, CharacterID, InventoryName, InventorySize)
                        VALUES (@CustomerGUID, @CharacterID, @InventoryName, @InventorySize) RETURNING CharInventoryID", inventoryArgs, transaction);

                object stagingInventoryID = inventory["charinventoryid"];
                List<IDictionary<string, object>> inventoryItems = items.Where(i => Equals(i["charinventoryid"], stagingInventoryID))
                    .Select(i => (IDictionary<string, object>)new Dictionary<string, object>(i) { ["charinventoryid"] = liveInventoryID })
                    .ToList();
                await Insert(live, transaction, "CharInventoryItems", inventoryItems, new { ids.CustomerGUID }, Array.Empty<string>());
            }

            await transaction.CommitAsync();
            return null;
        }

        private static async Task<List<IDictionary<string, object>>> Rows(NpgsqlConnection connection, NpgsqlTransaction transaction, string table, object args)
        {
            return (await connection.QueryAsync($"SELECT * FROM {table} WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID", args, transaction))
                .Cast<IDictionary<string, object>>().ToList();
        }

        /// <summary>Inserts rows as read from staging, with the live keys from keys (by column name) and without skipped columns.</summary>
        private static async Task Insert(NpgsqlConnection connection, NpgsqlTransaction transaction, string table,
            List<IDictionary<string, object>> rows, object keys, string[] skipped)
        {
            Dictionary<string, object> keyValues = keys.GetType().GetProperties()
                .ToDictionary(p => p.Name.ToLowerInvariant(), p => p.GetValue(keys));
            foreach (IDictionary<string, object> row in rows)
            {
                List<string> columns = row.Keys.Where(c => !skipped.Contains(c)).ToList();
                DynamicParameters values = new();
                foreach (string column in columns)
                {
                    values.Add("c_" + column, keyValues.TryGetValue(column, out object key) ? key : row[column]);
                }
                await connection.ExecuteAsync($"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(c => "@c_" + c))})",
                    values, transaction);
            }
        }
    }
}
