using Dapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Npgsql;
using System.Threading;
using System.Threading.Tasks;
using Dapper.Transaction;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSData.Models.Tables;
using OWSData.SQL;
using OWSShared.Options;
using PartyServiceApp.Protos;
using GuildServiceApp.Protos;
using System.Text.Json;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class CharactersRepository : ICharactersRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public CharactersRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private readonly AsyncLocal<ScopedDbConnection> _scopedConnection = new AsyncLocal<ScopedDbConnection>();

        // Debounce state for instance cleanup. Cleanup used to run inline (and awaited) on
        // every zone join, holding row locks on the critical path. It only needs to run
        // periodically, so we now trigger it at most once per interval per customer and do
        // not block the join on it. Shared across all (transient) repo instances.
        private static readonly ConcurrentDictionary<Guid, long> _lastInstanceCleanupTicks = new();

        private static int InstanceCleanupIntervalSeconds =>
            int.TryParse(Environment.GetEnvironmentVariable("OWS_INSTANCE_CLEANUP_INTERVAL_SECONDS"), out int value) && value > 0
                ? value
                : 60;

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        // Runs CleanUpInstances off the request path: skips if it ran within the interval,
        // claims the slot atomically so concurrent joins don't double-fire, and runs the
        // cleanup without awaiting so the join isn't blocked. Best-effort — the Instance
        // Launcher health monitor independently reaps idle/stale zone servers as a backstop.
        private void TriggerInstanceCleanupIfDue(Guid customerGUID)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long intervalTicks = TimeSpan.FromSeconds(InstanceCleanupIntervalSeconds).Ticks;
            long last = _lastInstanceCleanupTicks.GetOrAdd(customerGUID, 0);

            if (nowTicks - last < intervalTicks)
            {
                return;
            }

            // Only the caller that wins this CAS fires; a concurrent join that lost it skips.
            if (!_lastInstanceCleanupTicks.TryUpdate(customerGUID, nowTicks, last))
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await CleanUpInstances(customerGUID);
                }
                catch
                {
                    // Best-effort maintenance; deliberately swallowed so a cleanup failure
                    // never surfaces on a player's join. The health monitor is the backstop.
                }
            });
        }

        private DbConnection Connection => _scopedConnection.Value ??= new ScopedDbConnection(CreateConnection(), () => _scopedConnection.Value = null);

        private sealed class PartyStateRow
        {
            public string PartyGuid { get; set; }
            public bool RaidingParty { get; set; }
            public string PartyName { get; set; }
            public string PartyDescription { get; set; }
            public int ExpDistributionMode { get; set; }
            public int LootDistributionMode { get; set; }
            public string CharGuid { get; set; }
            public string CharName { get; set; }
            public bool PartyLeader { get; set; }
        }

        private static Guid? TryParseGuid(string value)
        {
            return Guid.TryParse(value, out Guid parsed) ? parsed : null;
        }

        private static PartyMemberInfo GetActorMember(PartyToSend partyRequest)
        {
            return partyRequest.PartyMembers.FirstOrDefault(member => member.PartyLeader)
                ?? partyRequest.PartyMembers.FirstOrDefault();
        }

        private static PartyToSend BuildPartyToSend(Guid customerGUID, PartyAction action, IEnumerable<PartyStateRow> rows)
        {
            PartyToSend party = new PartyToSend
            {
                CustomerGuid = customerGUID.ToString(),
                PartyAction = action,
                PartyInfo = new PartyInfo()
            };

            PartyStateRow firstRow = rows.FirstOrDefault();
            if (firstRow == null)
            {
                return party;
            }

            party.PartyInfo.PartyGuid = firstRow.PartyGuid;
            party.PartyInfo.RaidingParty = firstRow.RaidingParty;
            party.PartyInfo.PartyName = firstRow.PartyName ?? string.Empty;
            party.PartyInfo.PartyDescription = firstRow.PartyDescription ?? string.Empty;
            party.PartyInfo.ExpDistributionMode = firstRow.ExpDistributionMode;
            party.PartyInfo.LootDistributionMode = firstRow.LootDistributionMode;

            foreach (PartyStateRow row in rows)
            {
                party.PartyMembers.Add(new PartyMemberInfo
                {
                    CharGuid = row.CharGuid ?? string.Empty,
                    CharName = row.CharName ?? string.Empty,
                    PartyLeader = row.PartyLeader
                });
            }

            return party;
        }

        private static async Task<IEnumerable<PartyStateRow>> GetPartyState(DbConnection connection, IDbTransaction transaction, Guid customerGUID, Guid partyGuid)
        {
            return await connection.QueryAsync<PartyStateRow>(
                @"SELECT
                    p.PartyGuid::TEXT AS PartyGuid,
                    p.RaidingParty AS RaidingParty,
                    p.PartyName AS PartyName,
                    p.PartyDescription AS PartyDescription,
                    p.ExpDistributionMode AS ExpDistributionMode,
                    p.LootDistributionMode AS LootDistributionMode,
                    c.CharGuid::TEXT AS CharGuid,
                    c.CharName::TEXT AS CharName,
                    pm.PartyLeader AS PartyLeader
                FROM Party p
                INNER JOIN PartyMember pm ON pm.CustomerGUID = p.CustomerGUID AND pm.PartyID = p.PartyID
                INNER JOIN Characters c ON c.CustomerGUID = pm.CustomerGUID AND c.CharacterID = pm.CharacterID
                WHERE p.CustomerGUID = @CustomerGUID AND p.PartyGuid = @PartyGuid
                ORDER BY pm.PartyLeader DESC, c.CharName",
                new { CustomerGUID = customerGUID, PartyGuid = partyGuid },
                transaction);
        }

        private static async Task<int?> GetCharacterId(DbConnection connection, IDbTransaction transaction, Guid customerGUID, Guid? charGuid, string charName)
        {
            return await connection.QuerySingleOrDefaultAsync<int?>(
                @"SELECT CharacterID
                FROM Characters
                WHERE CustomerGUID = @CustomerGUID
                    AND (
                        (@CharGuid IS NOT NULL AND CharGuid = @CharGuid)
                        OR (@CharName IS NOT NULL AND CharName = @CharName)
                    )
                LIMIT 1",
                new { CustomerGUID = customerGUID, CharGuid = charGuid, CharName = charName },
                transaction);
        }

        private static async Task<int?> GetPartyId(DbConnection connection, IDbTransaction transaction, Guid customerGUID, Guid partyGuid)
        {
            return await connection.QuerySingleOrDefaultAsync<int?>(
                @"SELECT PartyID
                FROM Party
                WHERE CustomerGUID = @CustomerGUID AND PartyGuid = @PartyGuid",
                new { CustomerGUID = customerGUID, PartyGuid = partyGuid },
                transaction);
        }

        // excludePartyId lets a rename keep its own current name; PartyID is a positive serial, so the
        // default 0 excludes nothing.
        private static async Task<bool> IsPartyNameAvailable(DbConnection connection, IDbTransaction transaction, Guid customerGUID, string partyName, int excludePartyId = 0)
        {
            string normalizedPartyName = PartyNameGenerator.Normalize(partyName);
            if (!PartyNameGenerator.IsValid(normalizedPartyName))
            {
                return false;
            }

            int existingNameCount = await connection.QuerySingleAsync<int>(
                @"SELECT COUNT(*)
                FROM Party
                WHERE CustomerGUID = @CustomerGUID
                    AND LOWER(PartyName) = LOWER(@PartyName)
                    AND DisbandedAt IS NULL
                    AND PartyID <> @ExcludePartyID",
                new { CustomerGUID = customerGUID, PartyName = normalizedPartyName, ExcludePartyID = excludePartyId },
                transaction);

            return existingNameCount == 0;
        }

        private static async Task<string> GenerateAvailablePartyName(DbConnection connection, IDbTransaction transaction, Guid customerGUID)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                string candidate = PartyNameGenerator.CreateCandidate(attempt);
                if (await IsPartyNameAvailable(connection, transaction, customerGUID, candidate))
                {
                    return candidate;
                }
            }

            string fallback = PartyNameGenerator.CreateFallback();
            if (await IsPartyNameAvailable(connection, transaction, customerGUID, fallback))
            {
                return fallback;
            }

            throw new InvalidOperationException("Unable to generate a unique party name.");
        }

        public async Task AddCharacterToMapInstanceByCharName(Guid customerGUID, string characterName, int mapInstanceID)
        {
            using IDbConnection conn = CreateConnection();
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);
                parameters.Add("@MapInstanceID", mapInstanceID);

                var outputCharacter = await conn.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterIDByName,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.Text);

                var outputZone = await conn.QuerySingleOrDefaultAsync<Maps>(GenericQueries.GetZoneName,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.Text);

                if (outputCharacter?.CharacterId > 0)
                {
                    parameters.Add("@CharacterID", outputCharacter.CharacterId);
                    parameters.Add("@ZoneName", outputZone?.ZoneName);

                    await conn.ExecuteAsync(GenericQueries.RemoveCharacterFromAllInstances,
                        parameters,
                        transaction: transaction,
                        commandType: CommandType.Text);

                    await conn.ExecuteAsync(GenericQueries.AddCharacterToInstance,
                        parameters,
                        transaction: transaction,
                        commandType: CommandType.Text);

                    await conn.ExecuteAsync(GenericQueries.UpdateCharacterZone,
                        parameters,
                        transaction: transaction,
                        commandType: CommandType.Text);
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw new Exception("Database Exception in AddCharacterToMapInstanceByCharName!");
            }
        }

        public async Task ReleaseCharacterMapReservation(Guid customerGUID, string characterName, int mapInstanceID)
        {
            using IDbConnection conn = CreateConnection();
            conn.Open();

            var parameters = new DynamicParameters();
            parameters.Add("@CustomerGUID", customerGUID);
            parameters.Add("@CharName", characterName);
            parameters.Add("@MapInstanceID", mapInstanceID);

            var outputCharacter = await conn.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterIDByName,
                parameters,
                commandType: CommandType.Text);

            if (outputCharacter?.CharacterId > 0)
            {
                parameters.Add("@CharacterID", outputCharacter.CharacterId);

                await conn.ExecuteAsync(GenericQueries.RemoveCharacterFromInstance,
                    parameters,
                    commandType: CommandType.Text);
            }
        }

        public async Task AddOrUpdateCustomCharacterData(Guid customerGUID, AddOrUpdateCustomCharacterData addOrUpdateCustomCharacterData)
        {
            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", addOrUpdateCustomCharacterData.CharacterName);
                parameters.Add("@CustomFieldName", addOrUpdateCustomCharacterData.CustomFieldName);
                parameters.Add("@FieldValue", addOrUpdateCustomCharacterData.FieldValue);

                var outputCharacter = await Connection.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterIDByName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputCharacter?.CharacterId > 0)
                {
                    parameters.Add("@CharacterID", outputCharacter.CharacterId);

                    var hasCustomCharacterData = await Connection.QuerySingleOrDefaultAsync<int>(GenericQueries.HasCustomCharacterDataForField,
                        parameters,
                        commandType: CommandType.Text);

                    if (hasCustomCharacterData > 0)
                    {
                        await Connection.ExecuteAsync(GenericQueries.UpdateCharacterCustomDataField,
                            parameters,
                            commandType: CommandType.Text);
                    }
                    else
                    {
                        await Connection.ExecuteAsync(GenericQueries.AddCharacterCustomDataField,
                            parameters,
                            commandType: CommandType.Text);
                    }
                }
            }
        }

        public async Task<MapInstances> CheckMapInstanceStatus(Guid customerGUID, int mapInstanceID)
        {
            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@MapInstanceID", mapInstanceID);

                var outputObject = await Connection.QuerySingleOrDefaultAsync<MapInstances>(GenericQueries.GetMapInstanceStatus,
                    parameters,
                    commandType: CommandType.Text);

                if (outputObject == null)
                {
                    return new MapInstances();
                }

                return outputObject;
            }
        }

        public async Task CleanUpInstances(Guid customerGUID)
        {
            using IDbConnection conn = CreateConnection();
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharacterMinutes", 1); // TODO Add Configuration Parameter
                parameters.Add("@MapMinutes", 2); // TODO Add Configuration Parameter

                await transaction.ExecuteAsync(PostgresQueries.RemoveCharactersFromAllInactiveInstances,
                    parameters,
                    commandType: CommandType.Text);

                var outputMapInstances = await transaction.QueryAsync<int>(PostgresQueries.GetAllInactiveMapInstances,
                    parameters,
                    commandType: CommandType.Text);

                if (outputMapInstances.Any())
                {
                    parameters.Add("@MapInstances", outputMapInstances);

                    // Detach characters from the reaped instances themselves. An earlier copy-paste re-ran
                    // the LastAccess-based delete here, which left CharOnMapInstance rows pointing at deleted
                    // MapInstances orphaned forever (no FK), permanently inflating HardPlayerCap occupancy.
                    // Must use the PostgresQueries "= ANY(@MapInstances)" variants: the GenericQueries
                    // "IN @MapInstances" form only works on providers where Dapper text-expands the list.
                    await transaction.ExecuteAsync(PostgresQueries.RemoveCharacterFromInstances,
                        parameters,
                        commandType: CommandType.Text);

                    await transaction.ExecuteAsync(PostgresQueries.RemoveMapInstances,
                        parameters,
                        commandType: CommandType.Text);

                }
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                //Keep the provider error attached. Discarding it made a SQL syntax error, a deadlock and a
                //constraint violation all surface identically as a bare 500 with no detail, which is exactly
                //how the broken "IN @MapInstances" form above stayed hidden.
                throw new Exception("Database Exception in CleanUpInstances!", ex);
            }
        }

        public async Task<GetCharByCharName> GetCharByCharName(Guid customerGUID, string characterName)
        {
            IEnumerable<GetCharByCharName> outputCharacter;

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputCharacter = await Connection.QueryAsync<GetCharByCharName>(GenericQueries.GetCharByCharName,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputCharacter.FirstOrDefault();
        }

        public async Task<IEnumerable<CustomCharacterData>> GetCustomCharacterData(Guid customerGUID, string characterName)
        {
            IEnumerable<CustomCharacterData> outputCustomCharacterDataRows;

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputCustomCharacterDataRows = await Connection.QueryAsync<CustomCharacterData>(GenericQueries.GetCharacterCustomDataByName,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputCustomCharacterDataRows;
        }

        public async Task<JoinMapByCharName> JoinMapByCharName(Guid customerGUID, string characterName, string zoneName, int playerGroupType)
        {
            // Runs periodically off the request path instead of inline on every join.
            TriggerInstanceCleanupIfDue(customerGUID);

            using (DbConnection conn = CreateConnection())
            {
                await conn.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);
                parameters.Add("@ZoneName", zoneName);
                parameters.Add("@PlayerGroupType", playerGroupType);

                Maps outputMap = await conn.QuerySingleOrDefaultAsync<Maps>(GenericQueries.GetMapByZoneName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputMap == null)
                {
                    return new JoinMapByCharName()
                    {
                        Success = false,
                        ErrorMessage = $"Error finding Zone: {zoneName}",
                        ServerIP = "",
                        Port = 0,
                        WorldServerID = -1,
                        WorldServerIP = "",
                        WorldServerPort = 0,
                        MapInstanceID = 0,
                        MapNameToStart = "",
                        MapInstanceStatus = -1,
                        NeedToStartupMap = false,
                        EnableAutoLoopback = false,
                        NoPortForwarding = false
                    };
                }

                Characters outputCharacter = await conn.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterByName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputCharacter == null)
                {
                    return new JoinMapByCharName()
                    {
                        Success = false,
                        ErrorMessage = $"Error finding Character: {characterName}",
                        ServerIP = "",
                        Port = 0,
                        WorldServerID = -1,
                        WorldServerIP = "",
                        WorldServerPort = 0,
                        MapInstanceID = 0,
                        MapNameToStart = "",
                        MapInstanceStatus = -1,
                        NeedToStartupMap = false,
                        EnableAutoLoopback = false,
                        NoPortForwarding = false
                    };
                }

                //Region is a property of the account. Characters carries UserGUID, so this is one extra
                //PK lookup rather than a join - GetCharacterByName is SELECT * and nothing else needs Users.
                //A character with no owner (legacy rows) and a user who never picked a server both fall back
                //to the default region, so existing players keep landing exactly where they always did.
                string serverRegion = ServerRegions.Default;
                bool accountIsNetworkTestUser = false;

                if (outputCharacter.UserGuid.HasValue)
                {
                    (string PreferredServerRegion, bool IsInternalNetworkTestUser) account = await conn.QuerySingleOrDefaultAsync<(string, bool)>(GenericQueries.GetJoinSettingsByUserGUID,
                        new { CustomerGUID = customerGUID, UserGUID = outputCharacter.UserGuid.Value },
                        commandType: CommandType.Text);

                    serverRegion = ServerRegions.Normalize(account.PreferredServerRegion);
                    accountIsNetworkTestUser = account.IsInternalNetworkTestUser;
                }

                PlayerGroup outputPlayerGroup = new PlayerGroup();

                if (playerGroupType > 0)
                {
                    outputPlayerGroup = await conn.QuerySingleOrDefaultAsync<PlayerGroup>(GenericQueries.GetPlayerGroupIDByType,
                        parameters,
                        commandType: CommandType.Text) ?? new PlayerGroup();
                }
                else
                {
                    outputPlayerGroup.PlayerGroupId = 0;
                }

                parameters.Add("@IsInternalNetworkTestUser", outputCharacter.IsInternalNetworkTestUser);
                parameters.Add("@HardPlayerCap", outputMap.HardPlayerCap);
                parameters.Add("@PlayerGroupID", outputPlayerGroup.PlayerGroupId);
                parameters.Add("@MapID", outputMap.MapId);
                parameters.Add("@CharacterID", outputCharacter.CharacterId);
                parameters.Add("@ServerRegion", serverRegion);

                using IDbTransaction transaction = conn.BeginTransaction();

                try
                {
                    await conn.ExecuteAsync(PostgresQueries.AcquireMapAllocationLock,
                        parameters,
                        transaction: transaction,
                        commandType: CommandType.Text);

                    JoinMapByCharName outputObject = await conn.QuerySingleOrDefaultAsync<JoinMapByCharName>(PostgresQueries.GetZoneInstancesByZoneAndGroup,
                        parameters,
                        transaction: transaction,
                        commandType: CommandType.Text);

                    if (outputObject != null)
                    {
                        outputObject.NeedToStartupMap = false;
                        outputObject.MapNameToStart = outputMap.MapName;
                        outputObject.Success = true;
                        outputObject.ErrorMessage = "";
                    }
                    else
                    {
                        MapInstances outputMapInstance = await SpinUpInstance(conn, transaction, customerGUID, outputMap, outputPlayerGroup.PlayerGroupId, serverRegion);

                        if (outputMapInstance.MapInstanceId < 1)
                        {
                            transaction.Rollback();
                            return new JoinMapByCharName()
                            {
                                Success = false,
                                ErrorMessage = $"No active World Servers or available instance ports were found in region {serverRegion}.",
                                WorldServerID = -1,
                                MapInstanceStatus = -1,
                                NeedToStartupMap = false
                            };
                        }

                        var worldServerParameters = new
                        {
                            CustomerGUID = customerGUID,
                            WorldServerID = outputMapInstance.WorldServerId
                        };

                        WorldServers outputWorldServers = await conn.QuerySingleOrDefaultAsync<WorldServers>(GenericQueries.GetWorldByID,
                            worldServerParameters,
                            transaction: transaction,
                            commandType: CommandType.Text);

                        if (outputWorldServers == null)
                        {
                            transaction.Rollback();
                            return new JoinMapByCharName()
                            {
                                Success = false,
                                ErrorMessage = $"Error finding World Server: {outputMapInstance.WorldServerId}",
                                WorldServerID = -1,
                                MapInstanceStatus = -1,
                                NeedToStartupMap = false
                            };
                        }

                        outputObject = new JoinMapByCharName()
                        {
                            Success = true,
                            ErrorMessage = "",
                            NeedToStartupMap = true,
                            WorldServerID = outputMapInstance.WorldServerId,
                            ServerIP = outputWorldServers.ServerIp,
                            WorldServerIP = outputWorldServers.InternalServerIp,
                            WorldServerPort = outputWorldServers.Port,
                            Port = outputMapInstance.Port,
                            MapInstanceID = outputMapInstance.MapInstanceId,
                            MapInstanceStatus = outputMapInstance.Status,
                            MapNameToStart = outputMap.MapName
                        };
                    }

                    await ReserveCharacterMapInstance(conn, transaction, customerGUID, outputCharacter.CharacterId, outputObject.MapInstanceID);

                    transaction.Commit();

                    outputObject.ServerIP = GetClientServerIp(outputObject, outputCharacter, accountIsNetworkTestUser);
                    return outputObject;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task<MapInstances> SpinUpInstance(Guid customerGUID, string zoneName, int playerGroupId = 0)
        {
            using (DbConnection conn = CreateConnection())
            {
                await conn.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@ZoneName", zoneName);

                Maps outputMap = await conn.QuerySingleOrDefaultAsync<Maps>(GenericQueries.GetMapByZoneName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputMap == null)
                {
                    return new MapInstances { MapInstanceId = -1 };
                }

                using IDbTransaction transaction = conn.BeginTransaction();

                try
                {
                    await conn.ExecuteAsync(PostgresQueries.AcquireMapAllocationLock,
                        parameters,
                        transaction: transaction,
                        commandType: CommandType.Text);

                    //No player context on this path (no caller today), so it can only target the default region.
                    MapInstances outputMapInstance = await SpinUpInstance(conn, transaction, customerGUID, outputMap, playerGroupId, ServerRegions.Default);
                    transaction.Commit();
                    return outputMapInstance;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private async Task<MapInstances> SpinUpInstance(IDbConnection conn, IDbTransaction transaction, Guid customerGUID, Maps outputMap, int playerGroupId, string serverRegion)
        {
            var worldServerParameters = new
            {
                CustomerGUID = customerGUID,
                ZoneName = outputMap.ZoneName,
                PlayerGroupId = playerGroupId,
                ServerRegion = ServerRegions.Normalize(serverRegion)
            };

            List<WorldServers> outputWorldServers = (List<WorldServers>)await conn.QueryAsync<WorldServers>(GenericQueries.GetActiveWorldServersByLoad,
                worldServerParameters,
                transaction: transaction,
                commandType: CommandType.Text);

            foreach (var worldServer in outputWorldServers)
            {
                var portsInUse = await conn.QueryAsync<int>(GenericQueries.GetPortsInUseByWorldServer,
                    new
                    {
                        CustomerGUID = customerGUID,
                        WorldServerID = worldServer.WorldServerId
                    },
                    transaction: transaction,
                    commandType: CommandType.Text);

                int firstAvailable = Enumerable.Range(worldServer.StartingMapInstancePort, worldServer.MaxNumberOfInstances)
                    .Except(portsInUse)
                    .FirstOrDefault();

                if (firstAvailable >= worldServer.StartingMapInstancePort)
                {
                    int outputMapInstanceID = await conn.QuerySingleOrDefaultAsync<int>(PostgresQueries.AddMapInstance,
                        new
                        {
                            CustomerGUID = customerGUID,
                            WorldServerID = worldServer.WorldServerId,
                            MapID = outputMap.MapId,
                            Port = firstAvailable,
                            PlayerGroupID = playerGroupId
                        },
                        transaction: transaction,
                        commandType: CommandType.Text);

                    MapInstances outputMapInstances = await conn.QuerySingleOrDefaultAsync<MapInstances>(GenericQueries.GetMapInstance,
                        new
                        {
                            CustomerGUID = customerGUID,
                            MapInstanceID = outputMapInstanceID
                        },
                        transaction: transaction,
                        commandType: CommandType.Text);

                    return outputMapInstances ?? new MapInstances { MapInstanceId = -1 };
                }
            }

            return new MapInstances { MapInstanceId = -1 };
        }

        private static async Task ReserveCharacterMapInstance(IDbConnection conn, IDbTransaction transaction, Guid customerGUID, int characterID, int mapInstanceID)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CustomerGUID", customerGUID);
            parameters.Add("@CharacterID", characterID);
            parameters.Add("@MapInstanceID", mapInstanceID);

            await conn.ExecuteAsync(GenericQueries.RemoveCharacterFromInstance,
                parameters,
                transaction: transaction,
                commandType: CommandType.Text);

            await conn.ExecuteAsync(GenericQueries.AddCharacterToInstance,
                parameters,
                transaction: transaction,
                commandType: CommandType.Text);
        }

        private static string GetClientServerIp(JoinMapByCharName outputObject, Characters outputCharacter, bool accountIsNetworkTestUser)
        {
            if ((outputCharacter.Email?.Contains("@localhost") ?? false) || outputCharacter.IsInternalNetworkTestUser || accountIsNetworkTestUser)
            {
                return "127.0.0.1";
            }

            return outputObject.ServerIP;
        }

        // A zone server that no longer owns this character must not write to it. The handoff swaps the
        // CharOnMapInstance row inside AddCharacterToMapInstanceByCharName's transaction, so this flips
        // the moment the destination takes ownership. A character owned by nobody (logout, instance
        // cleanup) is still writable, so a late save from the last zone is not lost.
        private async Task<bool> IsStaleZoneWrite(Guid customerGUID, string characterName, int? callerZoneInstanceId)
        {
            if (!callerZoneInstanceId.HasValue || callerZoneInstanceId.Value <= 0)
            {
                return false;
            }

            var p = new DynamicParameters();
            p.Add("@CustomerGUID", customerGUID);
            p.Add("@CharName", characterName);
            p.Add("@CallerZoneInstanceID", callerZoneInstanceId.Value);

            return await Connection.ExecuteScalarAsync<bool>(PostgresQueries.IsCharacterOwnedByOtherZoneInstance, p);
        }

        public async Task<bool> UpdateCharacterStats(Guid customerGUID, string characterName, IEnumerable<UpdateCharacterStats> updateCharacterStats, int? callerZoneInstanceId = null)
        {
            using (Connection)
            {
                if (await IsStaleZoneWrite(customerGUID, characterName, callerZoneInstanceId))
                {
                    return false;
                }

                var statIds = updateCharacterStats.Select(s => s.StatIdentifier).ToArray();
                var statVals = updateCharacterStats.Select(s => s.Value).ToArray();

                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);
                p.Add("@StatIdentifiers", statIds, dbType: DbType.Object, direction: ParameterDirection.Input);
                p.Add("@StatValues", statVals, dbType: DbType.Object, direction: ParameterDirection.Input);

                await Connection.ExecuteAsync(GenericQueries.UpsertManyCharacterStats, p, commandType: CommandType.Text);
                return true;
            }
        }

        public async Task UpdatePosition(Guid customerGUID, string characterName, string mapName, float X, float Y, float Z, float RX, float RY, float RZ)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);
                p.Add("@MapName", mapName);
                p.Add("@X", X);
                p.Add("@Y", Y);
                p.Add("@Z", Z + 1);
                p.Add("@RX", RX);
                p.Add("@RY", RY);
                p.Add("@RZ", RZ);

                if (mapName != String.Empty)
                {
                    await Connection.ExecuteAsync(GenericQueries.UpdateCharacterPositionAndMap,
                        p,
                        commandType: CommandType.Text);
                }
                else
                {
                    await Connection.ExecuteAsync(GenericQueries.UpdateCharacterPosition,
                        p,
                        commandType: CommandType.Text);
                }

                await Connection.ExecuteAsync(PostgresQueries.UpdateUserLastAccess,
                    p,
                    commandType: CommandType.Text);
            }
        }

        public async Task PlayerLogout(Guid customerGUID, string characterName)
        {
            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                var outputCharacter = await Connection.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterIDByName,
                    parameters,
                    commandType: CommandType.Text);

                //QuerySingleOrDefaultAsync returns null when the character name is unknown (renamed,
                //deleted, or a stale name still held by a zone server). Logging out a character that
                //no longer exists is a no-op, not a 500.
                if (outputCharacter?.CharacterId > 0)
                {
                    parameters.Add("@CharacterID", outputCharacter.CharacterId);

                    await Connection.ExecuteAsync(GenericQueries.RemoveCharacterFromAllInstances,
                        parameters,
                        commandType: CommandType.Text);
                }
            }
        }

        public async Task AddAbilityToCharacter(Guid customerGUID, string abilityName, string characterName, int abilityLevel, string charHasAbilitiesCustomJSON)
        {
            using (Connection)
            {
                var parameters = new
                {
                    CustomerGUID = customerGUID,
                    AbilityName = abilityName,
                    CharacterName = characterName,
                    AbilityLevel = abilityLevel,
                    CharHasAbilitiesCustomJSON = charHasAbilitiesCustomJSON
                };

                var outputCharacterAbility = await Connection.QuerySingleOrDefaultAsync<GlobalData>(GenericQueries.GetCharacterAbilityByName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputCharacterAbility == null)
                {
                    await Connection.ExecuteAsync(PostgresQueries.AddAbilityToCharacter,
                        parameters,
                        commandType: CommandType.Text);
                }
            }
        }

        public async Task<CharacterSaveData> GetSaveDataByCharName(Guid customerGUID, string characterName)
        {
            CharacterSaveData outputCharacter = new CharacterSaveData();

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputCharacter.CharGuid = await Connection.QuerySingleOrDefaultAsync<string>(GenericQueries.GetCharGuidByCharName,
                    parameters,
                    commandType: CommandType.Text);

                outputCharacter.CharStats = await Connection.QueryAsync<CharacterStat>(GenericQueries.GetCharStatsByCharName,
                    parameters,
                    commandType: CommandType.Text);

                outputCharacter.CharQuests = await Connection.QueryAsync<CharacterQuest>(GenericQueries.GetCharQuestsByCharName,
                    parameters,
                    commandType: CommandType.Text);

                outputCharacter.CharInventory = await Connection.QueryAsync<CharacterInventory>(PostgresQueries.GetCharInventoryByCharName,
                    parameters,
                    commandType: CommandType.Text);

                outputCharacter.CharCurrency = await Connection.QueryAsync<CharacterCurrency>(PostgresQueries.GetCharInventoryByCharName,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputCharacter;
        }

        public async Task<IEnumerable<GetCharStatsByCharName>> GetCharStatsByCharName(Guid customerGUID, string characterName)
        {
            IEnumerable<GetCharStatsByCharName> outputCharacter = new List<GetCharStatsByCharName>();

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputCharacter = await Connection.QueryAsync<GetCharStatsByCharName>(GenericQueries.GetCharStatsByCharName,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputCharacter;
        }
        public async Task<IEnumerable<GetCharQuestsByCharName>> GetCharQuetsByCharName(Guid customerGUID, string characterName)
        {
            IEnumerable<GetCharQuestsByCharName> outputCharacter = new List<GetCharQuestsByCharName>();

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputCharacter = await Connection.QueryAsync<GetCharQuestsByCharName>(GenericQueries.GetCharQuestsByCharName,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputCharacter;
        }
        public async Task<IEnumerable<GetCharInventoryByCharName>> GetCharInventoryByCharName(Guid customerGUID, string characterName)
        {
            IEnumerable<GetCharInventoryByCharName> outputCharacter = new List<GetCharInventoryByCharName>();

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                IEnumerable<int> InventoryIDs = await Connection.QueryAsync<int>(GenericQueries.GetCharInventoryIDByCharName,
                parameters,
                commandType: CommandType.Text);

                if(!InventoryIDs.Any())
                {
                    return outputCharacter;
                }

                parameters.Add("@CharInventoryID", InventoryIDs.First());

                outputCharacter = await Connection.QueryAsync<GetCharInventoryByCharName>(PostgresQueries.GetCharInventoryByCharName,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputCharacter;
        }

        private sealed class CharNameLockRow
        {
            public int CharacterID { get; set; }
            public long EconomyRevision { get; set; }
            public int Gold { get; set; }
        }

        public async Task<UpdateCharacterInventoryResponse> UpdateCharacterInventory(Guid customerGUID, string characterName,
            IEnumerable<UpdateCharacterInventory> updateCharacterInventory, long? expectedRevision = null)
        {
            // A whole-bag rewrite is DELETE-then-INSERT, so it has to be one transaction. This ran
            // on an auto-committing connection before: a failure part-way through the insert loop
            // left the player holding a truncated bag with nothing to roll back to, and it is the
            // most frequent write in the game.
            // The character row is locked for the duration so the save serializes with the shop
            // economy transactions (Purchase/ClaimEscrow/VendorTrade) that rewrite the same table.
            using DbConnection conn = CreateConnection();
            await conn.OpenAsync();
            using DbTransaction tx = await conn.BeginTransactionAsync();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                CharNameLockRow character = await conn.QuerySingleOrDefaultAsync<CharNameLockRow>(
                    PlayerShopQueries.LockCharacterByNameForUpdate,
                    parameters,
                    transaction: tx, commandType: CommandType.Text);

                if (character == null)
                {
                    await tx.RollbackAsync();
                    return InventoryFail("bad_request");
                }

                // Opt-in optimistic guard. Serializing on the lock alone does not save a stale
                // caller: it would still write a pre-transaction bag over a shop op that committed
                // while it was queued. Only this check stops that, and it needs the caller to tell
                // us which revision the bag was computed from.
                if (expectedRevision.HasValue && expectedRevision.Value != character.EconomyRevision)
                {
                    await tx.RollbackAsync();
                    return InventoryFail("stale_revision", character.EconomyRevision);
                }

                IEnumerable<int> inventoryIDs = await conn.QueryAsync<int>(GenericQueries.GetCharInventoryIDByCharName,
                    parameters,
                    transaction: tx, commandType: CommandType.Text);

                // Character creation always inserts a CharInventory row, so a missing one means the
                // character is malformed. Reported rather than silently succeeding — a save that
                // writes nothing and claims success is how bag loss stays invisible.
                if (!inventoryIDs.Any())
                {
                    await tx.RollbackAsync();
                    return InventoryFail("no_inventory", character.EconomyRevision);
                }

                int charInventoryID = inventoryIDs.First();
                parameters.Add("@CharInventoryID", charInventoryID);

                await conn.ExecuteAsync(GenericQueries.DeleteCharacterInventoryItems,
                    parameters,
                    transaction: tx, commandType: CommandType.Text);

                foreach (UpdateCharacterInventory inventoryItem in updateCharacterInventory ?? Enumerable.Empty<UpdateCharacterInventory>())
                {
                    await conn.ExecuteAsync(PostgresQueries.InsertCharacterInventoryItem,
                        new
                        {
                            CustomerGUID = customerGUID,
                            CharInventoryID = charInventoryID,
                            inventoryItem.ItemIDTag,
                            inventoryItem.Quantity,
                            inventoryItem.InSlotNumber,
                            inventoryItem.CustomData,
                            InstanceId = inventoryItem.ParsedInstanceId
                        },
                        transaction: tx, commandType: CommandType.Text);
                }

                // Bump only for callers on the protocol. UpdateCharacterCurrency bumps
                // unconditionally and gets away with it because the zone server already reads the
                // revision back off that response; NewEconomyRevision here is a new field nothing
                // reads yet, so bumping on every autosave would strand the zone server's cached
                // revision and fail every following shop op with stale_revision.
                // Consequence, spelled out: on the default path a stale bag snapshot can still
                // overwrite a committed shop transaction. Closing that needs the zone server to
                // send ExpectedRevision — see docs/mvp-hardening-spec.md item 2.
                long newRevision = character.EconomyRevision;
                if (expectedRevision.HasValue)
                {
                    newRevision = await conn.ExecuteScalarAsync<long>(PlayerShopQueries.BumpRevisionByName,
                        parameters,
                        transaction: tx, commandType: CommandType.Text);
                }

                await tx.CommitAsync();
                return new UpdateCharacterInventoryResponse
                {
                    Success = true,
                    ErrorMessage = "",
                    ReasonCode = "",
                    NewEconomyRevision = newRevision
                };
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { /* connection already gone */ }
                throw;
            }
        }

        private static UpdateCharacterInventoryResponse InventoryFail(string reasonCode, long revision = 0) =>
            new UpdateCharacterInventoryResponse
            {
                Success = false,
                ErrorMessage = reasonCode,
                ReasonCode = reasonCode,
                NewEconomyRevision = revision
            };

        public async Task<UpdateCharacterCurrencyResponse> UpdateCharacterCurrency(Guid customerGUID, string characterName, int gold, long? expectedRevision = null)
        {
            // Participate in the economy-revision protocol instead of a blind overwrite: lock the
            // character row (serializing with shop Purchase/ClaimEscrow, which also FOR UPDATE the
            // row) and bump EconomyRevision on write, so a concurrent shop op detects this
            // out-of-band gold write via its optimistic guard rather than silently reading a
            // clobbered wallet. The new revision is returned to the zone server, which resyncs its
            // cached revision from it — without that resync every later shop op is stale_revision.
            using IDbConnection conn = CreateConnection();
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);
                parameters.Add("@Gold", gold);

                CharNameLockRow character = await conn.QuerySingleOrDefaultAsync<CharNameLockRow>(
                    PlayerShopQueries.LockCharacterByNameForUpdate,
                    parameters,
                    transaction: transaction, commandType: CommandType.Text);

                if (character == null)
                {
                    transaction.Rollback();
                    return new UpdateCharacterCurrencyResponse
                    {
                        Success = false,
                        ErrorMessage = "Character not found.",
                        ReasonCode = "bad_request"
                    };
                }

                // Opt-in optimistic guard, same reasoning as UpdateCharacterInventory. This write is
                // absolute, so without the guard a wallet computed before a purchase can be written
                // back over it: a buyer whose Purchase response was lost keeps its pre-purchase gold
                // locally, and the next save — the stale-revision resync or just the periodic
                // currency autosave — refunds the spend while the purchase row stays paid.
                if (expectedRevision.HasValue && expectedRevision.Value != character.EconomyRevision)
                {
                    transaction.Rollback();
                    return new UpdateCharacterCurrencyResponse
                    {
                        Success = true,
                        ReasonCode = "stale_revision",
                        NewEconomyRevision = character.EconomyRevision,
                        AuthoritativeGold = character.Gold
                    };
                }

                long newRevision = await transaction.ExecuteScalarAsync<long>(PlayerShopQueries.SetGoldAndBumpRevisionByName, parameters);
                transaction.Commit();
                return new UpdateCharacterCurrencyResponse
                {
                    Success = true,
                    NewEconomyRevision = newRevision,
                    AuthoritativeGold = gold
                };
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task UpdateCharacterClass(Guid customerGUID, string characterName, string className)
        {
            // No economy-revision locking here: ClassID is not currency and is not read-modify-write,
            // so a plain parameterized UPDATE is enough.
            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);
                parameters.Add("@ClassName", className);

                await Connection.ExecuteAsync(GenericQueries.UpdateCharacterClass,
                    parameters,
                    commandType: CommandType.Text);
            }
        }

        public async Task<SuccessAndErrorMessage> UpdateCharacterAbilities(Guid customerGUID, string charName, string abilitiesJson)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", charName);
                p.Add("@AbilitiesJson", abilitiesJson);

                await Connection.ExecuteAsync(
                    PostgresQueries.UpsertCharacterAbilitiesJson,
                    p,
                    commandType: CommandType.Text);
            }

            return new SuccessAndErrorMessage
            {
                Success = true,
                ErrorMessage = string.Empty
            };
        }

        public async Task<bool> UpdateCharacterQuests(Guid customerGUID, string characterName, IEnumerable<UpdateCharacterQuest> updateCharacterQuests, int? callerZoneInstanceId = null)
        {
            using (Connection)
            {
                if (await IsStaleZoneWrite(customerGUID, characterName, callerZoneInstanceId))
                {
                    return false;
                }

                foreach (UpdateCharacterQuest Quest in updateCharacterQuests)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@CharName", characterName);
                    p.Add("@QuestIDTag", Quest.QuestIDTag);
                    p.Add("@QuestJournalTagContainer", Quest.QuestJournalTagContainer);
                    p.Add("@CustomData", Quest.CustomData);


                    await Connection.ExecuteAsync(PostgresQueries.UpdateCharacterQuest, p);
                }

                return true;
            }
        }

        public async Task<PartyToSend> CreatePartyOrAddMember(Guid customerGUID, PartyToSend partyRequest)
        {
            if (partyRequest == null)
            {
                throw new ArgumentNullException(nameof(partyRequest));
            }

            PartyMemberInfo actor = GetActorMember(partyRequest);
            if (actor == null || string.IsNullOrWhiteSpace(actor.CharName))
            {
                throw new InvalidOperationException("A party actor is required.");
            }

            using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            using IDbTransaction transaction = connection.BeginTransaction();

            try
            {
                Guid partyGuid;
                string actorName = actor.CharName;
                int? actorCharacterId = await GetCharacterId(connection, transaction, customerGUID, TryParseGuid(actor.CharGuid), actor.CharName);
                if (actorCharacterId == null)
                {
                    throw new InvalidOperationException("Party actor character was not found.");
                }

                if (partyRequest.PartyAction == PartyAction.MessageTypeCreate)
                {
                    int existingPartyCount = await connection.QuerySingleAsync<int>(
                        @"SELECT COUNT(*) FROM PartyMember WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID",
                        new { CustomerGUID = customerGUID, CharacterID = actorCharacterId.Value },
                        transaction);

                    if (existingPartyCount > 0)
                    {
                        throw new InvalidOperationException("Character is already in a party.");
                    }

                    string partyName = PartyNameGenerator.Normalize(partyRequest.PartyInfo?.PartyName);
                    if (string.IsNullOrWhiteSpace(partyName))
                    {
                        partyName = await GenerateAvailablePartyName(connection, transaction, customerGUID);
                    }
                    else if (!await IsPartyNameAvailable(connection, transaction, customerGUID, partyName))
                    {
                        throw new InvalidOperationException("Party name is already in use.");
                    }

                    string partyDescription = string.IsNullOrWhiteSpace(partyRequest.PartyInfo?.PartyDescription)
                        ? null
                        : partyRequest.PartyInfo.PartyDescription.Trim();

                    int expDistributionMode = partyRequest.PartyInfo?.ExpDistributionMode ?? 0;
                    if (expDistributionMode != 0 && expDistributionMode != 1)
                    {
                        throw new InvalidOperationException("Exp distribution mode must be 0 (Individual) or 1 (Shared).");
                    }

                    int lootDistributionMode = partyRequest.PartyInfo?.LootDistributionMode ?? 0;
                    if (lootDistributionMode < 0 || lootDistributionMode > 3)
                    {
                        throw new InvalidOperationException("Loot distribution mode must be 0 (Individual), 1 (Shared), 2 (Leader), or 3 (Keep valuables).");
                    }

                    var createdParty = await connection.QuerySingleAsync(
                        @"INSERT INTO Party (CustomerGUID, PartyGuid, RaidingParty, PartyName, PartyDescription, ExpDistributionMode, LootDistributionMode)
                        VALUES (@CustomerGUID, gen_random_uuid(), @RaidingParty, @PartyName, @PartyDescription, CAST(@ExpDistributionMode AS SMALLINT), CAST(@LootDistributionMode AS SMALLINT))
                        RETURNING PartyID, PartyGuid",
                        new { CustomerGUID = customerGUID, RaidingParty = partyRequest.PartyInfo?.RaidingParty ?? false, PartyName = partyName, PartyDescription = partyDescription, ExpDistributionMode = expDistributionMode, LootDistributionMode = lootDistributionMode },
                        transaction);

                    int partyId = createdParty.partyid;
                    partyGuid = createdParty.partyguid;

                    await connection.ExecuteAsync(
                        @"INSERT INTO PartyMember (CustomerGUID, PartyID, CharacterID, PartyLeader, PartyRole)
                        VALUES (@CustomerGUID, @PartyID, @CharacterID, TRUE, 2)",
                        new { CustomerGUID = customerGUID, PartyID = partyId, CharacterID = actorCharacterId.Value },
                        transaction);

                    foreach (PartyMemberInfo member in partyRequest.PartyMembers.Where(member => !string.Equals(member.CharName, actorName, StringComparison.OrdinalIgnoreCase)))
                    {
                        await AddPartyMember(connection, transaction, customerGUID, partyId, member);
                    }

                    IEnumerable<PartyStateRow> createRows = await GetPartyState(connection, transaction, customerGUID, partyGuid);
                    transaction.Commit();
                    return BuildPartyToSend(customerGUID, partyRequest.PartyAction, createRows);
                }

                if (partyRequest.PartyInfo == null || !Guid.TryParse(partyRequest.PartyInfo.PartyGuid, out partyGuid))
                {
                    throw new InvalidOperationException("A valid party GUID is required.");
                }

                int? existingPartyId = await GetPartyId(connection, transaction, customerGUID, partyGuid);
                if (existingPartyId == null)
                {
                    throw new InvalidOperationException("Party was not found.");
                }

                int actorLeaderCount = await connection.QuerySingleAsync<int>(
                    @"SELECT COUNT(*)
                    FROM PartyMember
                    WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID AND CharacterID = @CharacterID AND PartyLeader = TRUE",
                    new { CustomerGUID = customerGUID, PartyID = existingPartyId.Value, CharacterID = actorCharacterId.Value },
                    transaction);

                if (actorLeaderCount == 0)
                {
                    throw new InvalidOperationException("Only the party leader can add members.");
                }

                foreach (PartyMemberInfo member in partyRequest.PartyMembers.Where(member => !string.Equals(member.CharName, actorName, StringComparison.OrdinalIgnoreCase)))
                {
                    await AddPartyMember(connection, transaction, customerGUID, existingPartyId.Value, member);
                }

                IEnumerable<PartyStateRow> addRows = await GetPartyState(connection, transaction, customerGUID, partyGuid);
                transaction.Commit();
                return BuildPartyToSend(customerGUID, partyRequest.PartyAction, addRows);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static async Task AddPartyMember(DbConnection connection, IDbTransaction transaction, Guid customerGUID, int partyId, PartyMemberInfo member)
        {
            int? memberCharacterId = await GetCharacterId(connection, transaction, customerGUID, TryParseGuid(member.CharGuid), member.CharName);
            if (memberCharacterId == null)
            {
                throw new InvalidOperationException("Party member character was not found.");
            }

            int? existingMemberPartyId = await connection.QuerySingleOrDefaultAsync<int?>(
                @"SELECT PartyID FROM PartyMember WHERE CustomerGUID = @CustomerGUID AND CharacterID = @CharacterID",
                new { CustomerGUID = customerGUID, CharacterID = memberCharacterId.Value },
                transaction);

            if (existingMemberPartyId != null)
            {
                if (existingMemberPartyId == partyId)
                {
                    return;
                }

                throw new InvalidOperationException("Character is already in another party.");
            }

            var partyCapacity = await connection.QuerySingleAsync(
                @"SELECT COUNT(pm.CharacterID) AS MemberCount, MAX(p.MaxMembers) AS MaxMembers
                FROM Party p
                LEFT JOIN PartyMember pm ON pm.CustomerGUID = p.CustomerGUID AND pm.PartyID = p.PartyID
                WHERE p.CustomerGUID = @CustomerGUID AND p.PartyID = @PartyID",
                new { CustomerGUID = customerGUID, PartyID = partyId },
                transaction);

            int memberCount = Convert.ToInt32(partyCapacity.membercount);
            int maxMembers = Convert.ToInt32(partyCapacity.maxmembers);
            if (memberCount >= maxMembers)
            {
                throw new InvalidOperationException("Party is full.");
            }

            await connection.ExecuteAsync(
                @"INSERT INTO PartyMember (CustomerGUID, PartyID, CharacterID, PartyLeader, PartyRole)
                VALUES (@CustomerGUID, @PartyID, @CharacterID, FALSE, 0)",
                new { CustomerGUID = customerGUID, PartyID = partyId, CharacterID = memberCharacterId.Value },
                transaction);
        }

        public async Task<PartyToSend> GetInitialPartySettings(Guid customerGUID, string charName)
        {
            using DbConnection connection = CreateConnection();

            IEnumerable<PartyStateRow> rows = await connection.QueryAsync<PartyStateRow>(
                @"SELECT
                    p.PartyGuid::TEXT AS PartyGuid,
                    p.RaidingParty AS RaidingParty,
                    p.PartyName AS PartyName,
                    p.PartyDescription AS PartyDescription,
                    p.ExpDistributionMode AS ExpDistributionMode,
                    p.LootDistributionMode AS LootDistributionMode,
                    c.CharGuid::TEXT AS CharGuid,
                    c.CharName::TEXT AS CharName,
                    pm.PartyLeader AS PartyLeader
                FROM PartyMember actorPm
                INNER JOIN Characters actor ON actor.CustomerGUID = actorPm.CustomerGUID AND actor.CharacterID = actorPm.CharacterID
                INNER JOIN Party p ON p.CustomerGUID = actorPm.CustomerGUID AND p.PartyID = actorPm.PartyID
                INNER JOIN PartyMember pm ON pm.CustomerGUID = p.CustomerGUID AND pm.PartyID = p.PartyID
                INNER JOIN Characters c ON c.CustomerGUID = pm.CustomerGUID AND c.CharacterID = pm.CharacterID
                WHERE actorPm.CustomerGUID = @CustomerGUID AND actor.CharName = @CharName
                ORDER BY pm.PartyLeader DESC, c.CharName",
                new
                {
                    CustomerGUID = customerGUID,
                    CharName = charName
                },
                commandType: CommandType.Text);

            return BuildPartyToSend(customerGUID, PartyAction.MessageTypeCreate, rows);
        }

        public async Task<PartyToSend> LeaveParty(Guid customerGUID, PartyToSend partyRequest)
        {
            if (partyRequest == null)
            {
                throw new ArgumentNullException(nameof(partyRequest));
            }

            if (partyRequest.PartyInfo == null || !Guid.TryParse(partyRequest.PartyInfo.PartyGuid, out Guid partyGuid))
            {
                throw new InvalidOperationException("A valid party GUID is required.");
            }

            PartyMemberInfo actor = partyRequest.PartyAction switch
            {
                PartyAction.MessageTypeKick or PartyAction.MessageTypeDismiss => partyRequest.PartyMembers.FirstOrDefault(member => member.PartyLeader),
                PartyAction.MessageTypeLeave when partyRequest.PartyMembers.Count == 1 => partyRequest.PartyMembers[0],
                PartyAction.MessageTypeLeave => throw new InvalidOperationException("Leave requires exactly the leaving member."),
                _ => GetActorMember(partyRequest)
            };
            if (actor == null || string.IsNullOrWhiteSpace(actor.CharName))
            {
                throw new InvalidOperationException("A party actor is required.");
            }

            using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            using IDbTransaction transaction = connection.BeginTransaction();

            try
            {
                int? partyId = await GetPartyId(connection, transaction, customerGUID, partyGuid);
                if (partyId == null)
                {
                    throw new InvalidOperationException("Party was not found.");
                }

                int? actorCharacterId = await GetCharacterId(connection, transaction, customerGUID, TryParseGuid(actor.CharGuid), actor.CharName);
                if (actorCharacterId == null)
                {
                    throw new InvalidOperationException("Party actor character was not found.");
                }

                bool actorIsLeader = await connection.QuerySingleAsync<bool>(
                    @"SELECT EXISTS (
                        SELECT 1 FROM PartyMember
                        WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID AND CharacterID = @CharacterID AND PartyLeader = TRUE
                    )",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, CharacterID = actorCharacterId.Value },
                    transaction);

                if (partyRequest.PartyAction == PartyAction.MessageTypeDismiss)
                {
                    if (!actorIsLeader)
                    {
                        throw new InvalidOperationException("Only the party leader can disband the party.");
                    }

                    IEnumerable<PartyStateRow> disbandRows = await GetPartyState(connection, transaction, customerGUID, partyGuid);
                    await connection.ExecuteAsync("DELETE FROM PartyMember WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                        new { CustomerGUID = customerGUID, PartyID = partyId.Value }, transaction);
                    await connection.ExecuteAsync("DELETE FROM Party WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                        new { CustomerGUID = customerGUID, PartyID = partyId.Value }, transaction);
                    transaction.Commit();
                    return BuildPartyToSend(customerGUID, partyRequest.PartyAction, disbandRows);
                }

                IEnumerable<PartyMemberInfo> membersToRemove = partyRequest.PartyAction switch
                {
                    PartyAction.MessageTypeKick => partyRequest.PartyMembers.Where(member => !member.PartyLeader),
                    PartyAction.MessageTypeLeave => new[] { actor },
                    _ => partyRequest.PartyMembers
                };

                IEnumerable<PartyStateRow> rows = Enumerable.Empty<PartyStateRow>();
                foreach (PartyMemberInfo member in membersToRemove)
                {
                    if (string.IsNullOrWhiteSpace(member.CharName))
                    {
                        continue;
                    }

                    int? memberCharacterId = await GetCharacterId(connection, transaction, customerGUID, TryParseGuid(member.CharGuid), member.CharName);
                    if (memberCharacterId == null)
                    {
                        throw new InvalidOperationException("Party member character was not found.");
                    }

                    if (partyRequest.PartyAction == PartyAction.MessageTypeKick && !actorIsLeader)
                    {
                        throw new InvalidOperationException("Only the party leader can remove other members.");
                    }

                    bool removedMemberWasLeader = await connection.QuerySingleAsync<bool>(
                        @"SELECT COALESCE((
                            SELECT PartyLeader FROM PartyMember
                            WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID AND CharacterID = @CharacterID
                        ), FALSE)",
                        new { CustomerGUID = customerGUID, PartyID = partyId.Value, CharacterID = memberCharacterId.Value },
                        transaction);

                    await connection.ExecuteAsync(
                        @"DELETE FROM PartyMember
                        WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID AND CharacterID = @CharacterID",
                        new { CustomerGUID = customerGUID, PartyID = partyId.Value, CharacterID = memberCharacterId.Value },
                        transaction);

                    int remainingCount = await connection.QuerySingleAsync<int>(
                        "SELECT COUNT(*) FROM PartyMember WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                        new { CustomerGUID = customerGUID, PartyID = partyId.Value },
                        transaction);

                    if (remainingCount == 0)
                    {
                        await connection.ExecuteAsync("DELETE FROM Party WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                            new { CustomerGUID = customerGUID, PartyID = partyId.Value }, transaction);
                        rows = Enumerable.Empty<PartyStateRow>();
                        continue;
                    }

                    if (removedMemberWasLeader)
                    {
                        int nextLeaderId = await connection.QuerySingleAsync<int>(
                            @"SELECT CharacterID
                            FROM PartyMember
                            WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID
                            ORDER BY CharacterID
                            LIMIT 1",
                            new { CustomerGUID = customerGUID, PartyID = partyId.Value },
                            transaction);

                        await connection.ExecuteAsync(
                            @"UPDATE PartyMember
                            SET PartyLeader = CharacterID = @NextLeaderID,
                                PartyRole = CASE WHEN CharacterID = @NextLeaderID THEN 2 ELSE 0 END
                            WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                            new { CustomerGUID = customerGUID, PartyID = partyId.Value, NextLeaderID = nextLeaderId },
                            transaction);
                    }

                    rows = await GetPartyState(connection, transaction, customerGUID, partyGuid);
                }

                transaction.Commit();
                return BuildPartyToSend(customerGUID, partyRequest.PartyAction, rows);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<PartyToSend> ChangePartyLeader(Guid customerGUID, PartyToSend partyRequest)
        {
            if (partyRequest == null)
            {
                throw new ArgumentNullException(nameof(partyRequest));
            }

            if (partyRequest.PartyInfo == null || !Guid.TryParse(partyRequest.PartyInfo.PartyGuid, out Guid partyGuid))
            {
                throw new InvalidOperationException("A valid party GUID is required.");
            }

            PartyMemberInfo actor = partyRequest.PartyMembers.FirstOrDefault(member => member.PartyLeader);
            PartyMemberInfo newLeader = partyRequest.PartyMembers.FirstOrDefault(member => !member.PartyLeader) ?? partyRequest.PartyMembers.FirstOrDefault();

            if (actor == null || newLeader == null || string.IsNullOrWhiteSpace(actor.CharName) || string.IsNullOrWhiteSpace(newLeader.CharName))
            {
                throw new InvalidOperationException("Current leader and new leader are required.");
            }

            using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            using IDbTransaction transaction = connection.BeginTransaction();

            try
            {
                int? partyId = await GetPartyId(connection, transaction, customerGUID, partyGuid);
                if (partyId == null)
                {
                    throw new InvalidOperationException("Party was not found.");
                }

                int? actorCharacterId = await GetCharacterId(connection, transaction, customerGUID, TryParseGuid(actor.CharGuid), actor.CharName);
                int? newLeaderCharacterId = await GetCharacterId(connection, transaction, customerGUID, TryParseGuid(newLeader.CharGuid), newLeader.CharName);

                if (actorCharacterId == null || newLeaderCharacterId == null)
                {
                    throw new InvalidOperationException("Current leader and new leader are required.");
                }

                bool actorIsLeader = await connection.QuerySingleAsync<bool>(
                    @"SELECT EXISTS (
                        SELECT 1 FROM PartyMember
                        WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID AND CharacterID = @CharacterID AND PartyLeader = TRUE
                    )",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, CharacterID = actorCharacterId.Value },
                    transaction);

                if (!actorIsLeader)
                {
                    throw new InvalidOperationException("Only the party leader can change party leader.");
                }

                bool newLeaderInParty = await connection.QuerySingleAsync<bool>(
                    @"SELECT EXISTS (
                        SELECT 1 FROM PartyMember
                        WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID AND CharacterID = @CharacterID
                    )",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, CharacterID = newLeaderCharacterId.Value },
                    transaction);

                if (!newLeaderInParty)
                {
                    throw new InvalidOperationException("New party leader is not in this party.");
                }

                await connection.ExecuteAsync(
                    @"UPDATE PartyMember
                    SET PartyLeader = CharacterID = @NewLeaderID,
                        PartyRole = CASE WHEN CharacterID = @NewLeaderID THEN 2 ELSE 0 END
                    WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, NewLeaderID = newLeaderCharacterId.Value },
                    transaction);

                IEnumerable<PartyStateRow> rows = await GetPartyState(connection, transaction, customerGUID, partyGuid);
                transaction.Commit();
                return BuildPartyToSend(customerGUID, partyRequest.PartyAction, rows);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> IsPartyNameAvailable(Guid customerGUID, string partyName)
        {
            using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            return await IsPartyNameAvailable(connection, null, customerGUID, partyName);
        }

        public async Task<string> GenerateAvailablePartyName(Guid customerGUID)
        {
            using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            return await GenerateAvailablePartyName(connection, null, customerGUID);
        }

        public async Task<PartyToSend> UpdatePartyName(Guid customerGUID, Guid partyGuid, string actorCharName, string actorCharGuid, string partyName)
        {
            if (partyGuid == Guid.Empty)
            {
                throw new InvalidOperationException("A valid party GUID is required.");
            }

            if (string.IsNullOrWhiteSpace(actorCharName) && string.IsNullOrWhiteSpace(actorCharGuid))
            {
                throw new InvalidOperationException("An acting character is required.");
            }

            string normalizedPartyName = PartyNameGenerator.Normalize(partyName);
            if (!PartyNameGenerator.IsValid(normalizedPartyName))
            {
                throw new InvalidOperationException($"Party name must be 1 to {PartyNameGenerator.MaxPartyNameLength} characters.");
            }

            using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            using IDbTransaction transaction = connection.BeginTransaction();

            try
            {
                int? partyId = await GetPartyId(connection, transaction, customerGUID, partyGuid);
                if (partyId == null)
                {
                    throw new InvalidOperationException("Party was not found.");
                }

                int? actorCharacterId = await GetCharacterId(connection, transaction, customerGUID, TryParseGuid(actorCharGuid), actorCharName);
                if (actorCharacterId == null)
                {
                    throw new InvalidOperationException("Party actor character was not found.");
                }

                bool actorIsLeader = await connection.QuerySingleAsync<bool>(
                    @"SELECT EXISTS (
                        SELECT 1
                        FROM PartyMember
                        WHERE CustomerGUID = @CustomerGUID
                            AND PartyID = @PartyID
                            AND CharacterID = @CharacterID
                            AND PartyLeader = TRUE
                    )",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, CharacterID = actorCharacterId.Value },
                    transaction);

                if (!actorIsLeader)
                {
                    throw new InvalidOperationException("Only the party leader can rename the party.");
                }

                if (!await IsPartyNameAvailable(connection, transaction, customerGUID, normalizedPartyName, partyId.Value))
                {
                    throw new InvalidOperationException("Party name is already in use.");
                }

                await connection.ExecuteAsync(
                    @"UPDATE Party
                    SET PartyName = @PartyName,
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, PartyName = normalizedPartyName },
                    transaction);

                IEnumerable<PartyStateRow> rows = await GetPartyState(connection, transaction, customerGUID, partyGuid);
                transaction.Commit();
                return BuildPartyToSend(customerGUID, PartyAction.MessageTypeUpdateInfo, rows);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<PartyToSend> UpdatePartyDescription(Guid customerGUID, Guid partyGuid, string actorCharName, string actorCharGuid, string partyDescription)
        {
            if (partyGuid == Guid.Empty)
            {
                throw new InvalidOperationException("A valid party GUID is required.");
            }

            if (string.IsNullOrWhiteSpace(actorCharName) && string.IsNullOrWhiteSpace(actorCharGuid))
            {
                throw new InvalidOperationException("An acting character is required.");
            }

            using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            using IDbTransaction transaction = connection.BeginTransaction();

            try
            {
                int? partyId = await GetPartyId(connection, transaction, customerGUID, partyGuid);
                if (partyId == null)
                {
                    throw new InvalidOperationException("Party was not found.");
                }

                int? actorCharacterId = await GetCharacterId(connection, transaction, customerGUID, TryParseGuid(actorCharGuid), actorCharName);
                if (actorCharacterId == null)
                {
                    throw new InvalidOperationException("Party actor character was not found.");
                }

                bool actorIsLeader = await connection.QuerySingleAsync<bool>(
                    @"SELECT EXISTS (
                        SELECT 1
                        FROM PartyMember
                        WHERE CustomerGUID = @CustomerGUID
                            AND PartyID = @PartyID
                            AND CharacterID = @CharacterID
                            AND PartyLeader = TRUE
                    )",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, CharacterID = actorCharacterId.Value },
                    transaction);

                if (!actorIsLeader)
                {
                    throw new InvalidOperationException("Only the party leader can update party description.");
                }

                string normalizedDescription = string.IsNullOrWhiteSpace(partyDescription)
                    ? null
                    : partyDescription.Trim();

                await connection.ExecuteAsync(
                    @"UPDATE Party
                    SET PartyDescription = @PartyDescription,
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, PartyDescription = normalizedDescription },
                    transaction);

                IEnumerable<PartyStateRow> rows = await GetPartyState(connection, transaction, customerGUID, partyGuid);
                transaction.Commit();
                return BuildPartyToSend(customerGUID, PartyAction.MessageTypeUpdateInfo, rows);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<PartyToSend> UpdatePartyExpDistribution(Guid customerGUID, Guid partyGuid, string actorCharName, string actorCharGuid, int expDistributionMode)
        {
            if (partyGuid == Guid.Empty)
            {
                throw new InvalidOperationException("A valid party GUID is required.");
            }

            if (string.IsNullOrWhiteSpace(actorCharName) && string.IsNullOrWhiteSpace(actorCharGuid))
            {
                throw new InvalidOperationException("An acting character is required.");
            }

            if (expDistributionMode != 0 && expDistributionMode != 1)
            {
                throw new InvalidOperationException("Exp distribution mode must be 0 (Individual) or 1 (Shared).");
            }

            using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            using IDbTransaction transaction = connection.BeginTransaction();

            try
            {
                int? partyId = await GetPartyId(connection, transaction, customerGUID, partyGuid);
                if (partyId == null)
                {
                    throw new InvalidOperationException("Party was not found.");
                }

                int? actorCharacterId = await GetCharacterId(connection, transaction, customerGUID, TryParseGuid(actorCharGuid), actorCharName);
                if (actorCharacterId == null)
                {
                    throw new InvalidOperationException("Party actor character was not found.");
                }

                bool actorIsLeader = await connection.QuerySingleAsync<bool>(
                    @"SELECT EXISTS (
                        SELECT 1
                        FROM PartyMember
                        WHERE CustomerGUID = @CustomerGUID
                            AND PartyID = @PartyID
                            AND CharacterID = @CharacterID
                            AND PartyLeader = TRUE
                    )",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, CharacterID = actorCharacterId.Value },
                    transaction);

                if (!actorIsLeader)
                {
                    throw new InvalidOperationException("Only the party leader can update exp distribution.");
                }

                await connection.ExecuteAsync(
                    @"UPDATE Party
                    SET ExpDistributionMode = CAST(@ExpDistributionMode AS SMALLINT),
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, ExpDistributionMode = expDistributionMode },
                    transaction);

                IEnumerable<PartyStateRow> rows = await GetPartyState(connection, transaction, customerGUID, partyGuid);
                transaction.Commit();
                return BuildPartyToSend(customerGUID, PartyAction.MessageTypeUpdateInfo, rows);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<PartyToSend> UpdatePartyLootDistribution(Guid customerGUID, Guid partyGuid, string actorCharName, string actorCharGuid, int lootDistributionMode)
        {
            if (partyGuid == Guid.Empty)
            {
                throw new InvalidOperationException("A valid party GUID is required.");
            }

            if (string.IsNullOrWhiteSpace(actorCharName) && string.IsNullOrWhiteSpace(actorCharGuid))
            {
                throw new InvalidOperationException("An acting character is required.");
            }

            if (lootDistributionMode < 0 || lootDistributionMode > 3)
            {
                throw new InvalidOperationException("Loot distribution mode must be 0 (Individual), 1 (Shared), 2 (Leader), or 3 (Keep valuables).");
            }

            using DbConnection connection = CreateConnection();
            await connection.OpenAsync();
            using IDbTransaction transaction = connection.BeginTransaction();

            try
            {
                int? partyId = await GetPartyId(connection, transaction, customerGUID, partyGuid);
                if (partyId == null)
                {
                    throw new InvalidOperationException("Party was not found.");
                }

                int? actorCharacterId = await GetCharacterId(connection, transaction, customerGUID, TryParseGuid(actorCharGuid), actorCharName);
                if (actorCharacterId == null)
                {
                    throw new InvalidOperationException("Party actor character was not found.");
                }

                bool actorIsLeader = await connection.QuerySingleAsync<bool>(
                    @"SELECT EXISTS (
                        SELECT 1
                        FROM PartyMember
                        WHERE CustomerGUID = @CustomerGUID
                            AND PartyID = @PartyID
                            AND CharacterID = @CharacterID
                            AND PartyLeader = TRUE
                    )",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, CharacterID = actorCharacterId.Value },
                    transaction);

                if (!actorIsLeader)
                {
                    throw new InvalidOperationException("Only the party leader can update loot distribution.");
                }

                await connection.ExecuteAsync(
                    @"UPDATE Party
                    SET LootDistributionMode = CAST(@LootDistributionMode AS SMALLINT),
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE CustomerGUID = @CustomerGUID AND PartyID = @PartyID",
                    new { CustomerGUID = customerGUID, PartyID = partyId.Value, LootDistributionMode = lootDistributionMode },
                    transaction);

                IEnumerable<PartyStateRow> rows = await GetPartyState(connection, transaction, customerGUID, partyGuid);
                transaction.Commit();
                return BuildPartyToSend(customerGUID, PartyAction.MessageTypeUpdateInfo, rows);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<GuildToSend> CreateGuildOrAddMember(Guid customerGUID, GuildToSend guildRequest)
        {
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@GuildName", guildRequest.GuildInfo.GuildName);

                    if (guildRequest.GuildAction == GuildAction.MessageTypeCreate)
                    {
                        
                        guildRequest.GuildInfo = await Connection.QuerySingleAsync<GuildInfo>("AddNewGuild",
                        p,
                        commandType: CommandType.StoredProcedure);
                        
                    }
                    
                    p.Add("@GuildGuid", Guid.Parse(guildRequest.GuildInfo.GuildGuid));

                    IEnumerable<GuildMemberInfo> guildMembersToAdd = guildRequest.GuildMembers.Clone();
                    guildRequest.GuildMembers.Clear();
                    GuildMemberInfo LastGuildMember = guildMembersToAdd.LastOrDefault();
                    foreach (GuildMemberInfo guildMember in guildMembersToAdd)
                    {
                        p.Add("@CharacterName", guildMember.CharName);
                        p.Add("@CharacterGUID", Guid.Parse(guildMember.CharGuid));
                        p.Add("@GuildRank", guildMember.GuildRank);

                        if(guildMember.Equals(LastGuildMember))
                        {
                            guildRequest.GuildMembers.Add(await Connection.QueryAsync<GuildMemberInfo>("AddNewGuildMember",
                            p,
                            commandType: CommandType.StoredProcedure));
                            break;
                        }
                        await Connection.QueryAsync<GuildMemberInfo>("AddNewGuildMember",
                        p,
                        commandType: CommandType.StoredProcedure);
                    }
                    
                }

            }
            catch (Exception ex)
            {
            }
            return guildRequest;
        }
        public async Task<GuildToSend> GetInitialGuildSettings(Guid customerGUID, string charName)
        {
            GuildToSend guildRequest = new GuildToSend();
            guildRequest.GuildInfo = new GuildInfo();
            guildRequest.GuildAction = GuildAction.MessageTypeInitial;
            guildRequest.GuildAbility = new GuildAbility();
            guildRequest.GuildBank = new GuildStorage();
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@CharName", charName);

                    
                    int guildId = await Connection.QuerySingleAsync<int>(GenericQueries.GetGuildId,
                    p,
                    commandType: CommandType.Text);

                    p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@GuildID", guildId);

                    guildRequest.GuildMembers.Add(await Connection.QueryAsync<GuildMemberInfo>("GetInitialGuildMembers",
                                                p,
                                                commandType: CommandType.StoredProcedure));

                    guildRequest.GuildInfo = await Connection.QuerySingleAsync<GuildInfo>("GetInitialGuildSettings",
                    p,
                    commandType: CommandType.StoredProcedure);

                    guildRequest.GuildAbility.GuildAbilities.Add(await Connection.QueryAsync<int>("GetInitialGuildAbilities",
                    p,
                    commandType: CommandType.StoredProcedure));


                }
            }
            catch (Exception ex)
            {
                guildRequest.GuildError = ex.ToString();
            }
            guildRequest.GuildInfo.GuildGuid = guildRequest.GuildInfo.GuildGuid.Replace("-", "");
            return guildRequest;
        }

        public async Task<GuildToSend> AddGuildAbilities(Guid customerGUID, GuildToSend guildInfo)
        {
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@GuildGuid", Guid.Parse(guildInfo.GuildInfo.GuildGuid));

                    int LastAbilityId = guildInfo.GuildAbility.GuildAbilities.LastOrDefault();
                    foreach(int GuildAbilityId in guildInfo.GuildAbility.GuildAbilities)
                    {
                        p.Add("@GuildAbilityId", GuildAbilityId);

                        await Connection.QueryAsync<int>("AddNewGuildAbility",
                        p,
                        commandType: CommandType.StoredProcedure);
                        
                    }

                    p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@GuildGuid", Guid.Parse(guildInfo.GuildInfo.GuildGuid));

                    guildInfo.GuildMembers.Add(await Connection.QueryAsync<GuildMemberInfo>("GetGuildMembers",
                   p,
                   commandType: CommandType.StoredProcedure));
                }
            }
            catch (Exception ex)
            {

            }
            return guildInfo;
        }

        public async Task AddQuestListToDatabase(Guid customerGUID, IEnumerable<AddQuestListToDabase> addQuestListToDabase)
        {
            using (Connection)
            {
                foreach (AddQuestListToDabase QuestToAdd in addQuestListToDabase)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@CustomerGUID", customerGUID);
                    parameters.Add("@QuestIDTag", QuestToAdd.QuestIDTag);
                    parameters.Add("@QuestOverview", QuestToAdd.QuestOverview);
                    parameters.Add("@QuestTasks", QuestToAdd.QuestTasks);
                    parameters.Add("@QuestClassName", QuestToAdd.QuestClassName);
                    parameters.Add("@CustomData", QuestToAdd.CustomData);

                    await Connection.ExecuteAsync(PostgresQueries.AddQuestToDatabase, parameters);
                }
            }
        }

        
        public async Task<IEnumerable<GetQuestsFromDb>> GetQuestsFromDatabase(Guid customerGUID)
        {
            IEnumerable<GetQuestsFromDb> QuestsFromDb;
            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);

                QuestsFromDb = await Connection.QueryAsync<GetQuestsFromDb>(GenericQueries.GetAllQuests,
                    parameters,
                    commandType: CommandType.Text);
            }

            return QuestsFromDb;
        }

        ///////////////////////// For abilities

        // public async Task<IEnumerable<Abilities>> GetAbilities(Guid customerGUID)
        // {
        //     IEnumerable<Abilities> outputGetAbilities;

        //     using (Connection)
        //     {
        //         var parameters = new DynamicParameters();
        //         parameters.Add("@CustomerGUID", customerGUID);

        //         outputGetAbilities = await Connection.QueryAsync<Abilities>(GenericQueries.GetAbilities,
        //             parameters,
        //             commandType: CommandType.Text);
        //     }

        //     return outputGetAbilities;
        // }

        public async Task<IEnumerable<CharacterAbilityDto>> GetCharacterAbilities(Guid customerGUID, string characterName)
        {
            IEnumerable<CharacterAbilityDto> outputGetCharacterAbilities;

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputGetCharacterAbilities = await Connection.QueryAsync<CharacterAbilityDto>(GenericQueries.GetCharacterAbilities,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputGetCharacterAbilities;
        }

        // public async Task<IEnumerable<GetAbilityBars>> GetAbilityBars(Guid customerGUID, string characterName)
        // {
        //     IEnumerable<GetAbilityBars> outputGetAbilityBars;

        //     using (Connection)
        //     {
        //         var parameters = new DynamicParameters();
        //         parameters.Add("@CustomerGUID", customerGUID);
        //         parameters.Add("@CharName", characterName);

        //         outputGetAbilityBars = await Connection.QueryAsync<GetAbilityBars>(GenericQueries.GetCharacterAbilityBars,
        //             parameters,
        //             commandType: CommandType.Text);
        //     }

        //     return outputGetAbilityBars;
        // }

        // public async Task<IEnumerable<GetAbilityBarsAndAbilities>> GetAbilityBarsAndAbilities(Guid customerGUID, string characterName)
        // {
        //     IEnumerable<GetAbilityBarsAndAbilities> outputGetAbilityBarsAndAbilities;

        //     using (Connection)
        //     {
        //         var parameters = new DynamicParameters();
        //         parameters.Add("@CustomerGUID", customerGUID);
        //         parameters.Add("@CharName", characterName);

        //         outputGetAbilityBarsAndAbilities = await Connection.QueryAsync<GetAbilityBarsAndAbilities>(GenericQueries.GetCharacterAbilityBarsAndAbilities,
        //             parameters,
        //             commandType: CommandType.Text);
        //     }

        //     return outputGetAbilityBarsAndAbilities;
        // }

        public async Task RemoveAbilityFromCharacter(Guid customerGUID, string abilityName, string characterName)
        {
            using (Connection)
            {
                var parameters = new
                {
                    CustomerGUID = customerGUID,
                    AbilityName = abilityName,
                    CharacterName = characterName
                };

                await Connection.ExecuteAsync(PostgresQueries.RemoveAbilityFromCharacter, parameters);
            }
        }

        public async Task UpdateAbilityOnCharacter(Guid customerGUID, string abilityName, string characterName, int abilityLevel, string charHasAbilitiesCustomJSON)
        {
            using (Connection)
            {
                var parameters = new
                {
                    CustomerGUID = customerGUID,
                    AbilityName = abilityName,
                    CharacterName = characterName,
                    AbilityLevel = abilityLevel,
                    CharHasAbilitiesCustomJSON = charHasAbilitiesCustomJSON
                };

                await Connection.ExecuteAsync(PostgresQueries.UpdateAbilityOnCharacter, parameters);
            }
        }

        Task ICharactersRepository.UpdateCharacterAbilities(Guid customerGUID, string characterName, string characterAbilities)
        {
            return UpdateCharacterAbilities(customerGUID, characterName, characterAbilities);
        }

        public async Task<IEnumerable<AdminCharacterSummary>> GetCharactersForUser(Guid customerGUID, Guid userGUID)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@UserGUID", userGUID);

                return await Connection.QueryAsync<AdminCharacterSummary>(GenericQueries.GetCharactersForUser,
                    p,
                    commandType: CommandType.Text);
            }
        }

        public async Task<IEnumerable<AdminCharacterSummary>> SearchCharacters(Guid customerGUID, string searchText)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                // Passed as a parameter, so the wildcards are data rather than SQL. An empty
                // search matches everything, capped at 200 rows inside the query itself.
                p.Add("@SearchPattern", "%" + (searchText ?? string.Empty).Trim().ToLowerInvariant() + "%");

                return await Connection.QueryAsync<AdminCharacterSummary>(GenericQueries.SearchCharacters,
                    p,
                    commandType: CommandType.Text);
            }
        }

        public async Task<SuccessAndErrorMessage> SetCharacterFlags(Guid customerGUID, int characterID, bool? isAdmin, bool? isModerator, bool? isInternalNetworkTestUser)
        {
            SuccessAndErrorMessage outputObject = new SuccessAndErrorMessage();

            if (!isAdmin.HasValue && !isModerator.HasValue && !isInternalNetworkTestUser.HasValue)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = "No flags supplied.";

                return outputObject;
            }

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@CharacterID", characterID);
                    // Explicit DbType: a null bool? otherwise reaches Npgsql as an untyped
                    // DBNull and the COALESCE fails with "could not determine data type".
                    p.Add("@IsAdmin", isAdmin, DbType.Boolean);
                    p.Add("@IsModerator", isModerator, DbType.Boolean);
                    p.Add("@IsInternalNetworkTestUser", isInternalNetworkTestUser, DbType.Boolean);

                    int rowsAffected = await Connection.ExecuteAsync(GenericQueries.UpdateCharacterFlags,
                        p,
                        commandType: CommandType.Text);

                    if (rowsAffected < 1)
                    {
                        outputObject.Success = false;
                        outputObject.ErrorMessage = "Character not found.";

                        return outputObject;
                    }
                }

                outputObject.Success = true;
                outputObject.ErrorMessage = "";

                return outputObject;
            }
            catch (Exception ex)
            {
                outputObject.Success = false;
                outputObject.ErrorMessage = ex.Message;

                return outputObject;
            }
        }
    }
}
