using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Transaction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.Tables;
using OWSData.SQL;
using PartyServiceApp.Protos;
using GuildServiceApp.Protos;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.MSSQL
{
    public class CharactersRepository : ICharactersRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public CharactersRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private IDbConnection Connection => new SqlConnection(_storageOptions.Value.OWSDBConnectionString);

        public async Task AddCharacterToMapInstanceByCharName(Guid customerGUID, string characterName, int mapInstanceID)
        {
            IDbConnection conn = Connection;
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);
                parameters.Add("@MapInstanceID", mapInstanceID);

                var outputCharacter = await Connection.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterIDByName,
                    parameters,
                    commandType: CommandType.Text);

                var outputZone = await Connection.QuerySingleOrDefaultAsync<Maps>(GenericQueries.GetZoneName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputCharacter.CharacterId > 0)
                {
                    parameters.Add("@CharacterID", outputCharacter.CharacterId);
                    parameters.Add("@ZoneName", outputZone.ZoneName);

                    await Connection.ExecuteAsync(GenericQueries.RemoveCharacterFromAllInstances,
                        parameters,
                        commandType: CommandType.Text);

                    await Connection.ExecuteAsync(GenericQueries.AddCharacterToInstance,
                        parameters,
                        commandType: CommandType.Text);

                    await Connection.ExecuteAsync(GenericQueries.UpdateCharacterZone,
                        parameters,
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
            using IDbConnection conn = Connection;
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

                if (outputCharacter.CharacterId > 0)
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
            IDbConnection conn = Connection;
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharacterMinutes", -1); // TODO Add Configuration Parameter
                parameters.Add("@MapMinutes", -2); // TODO Add Configuration Parameter

                await transaction.ExecuteAsync(MSSQLQueries.RemoveCharactersFromAllInactiveInstances,
                    parameters,
                    commandType: CommandType.Text);

                var outputMapInstances = await transaction.QueryAsync<int>(MSSQLQueries.GetAllInactiveMapInstances,
                    parameters,
                    commandType: CommandType.Text);

                if (outputMapInstances.Any())
                {
                    parameters.Add("@MapInstances", outputMapInstances);

                    await transaction.ExecuteAsync(GenericQueries.RemoveCharacterFromInstances,
                        parameters,
                        commandType: CommandType.Text);

                    await transaction.ExecuteAsync(GenericQueries.RemoveMapInstances,
                        parameters,
                        commandType: CommandType.Text);

                }
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                //Keep the provider error attached; without it the caller only sees a bare 500 with no detail.
                throw new Exception("Database Exception in CleanUpInstances!", ex);
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

                outputCharacter.CharInventory = await Connection.QueryAsync<CharacterInventory>(GenericQueries.GetCharInventoryByCharName,
                    parameters,
                    commandType: CommandType.Text);

                outputCharacter.CharCurrency = await Connection.QueryAsync<CharacterCurrency>(GenericQueries.GetCharInventoryByCharName,
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

                outputCharacter = await Connection.QueryAsync<GetCharInventoryByCharName>(GenericQueries.GetCharInventoryByCharName,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputCharacter;
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
        public async Task<JoinMapByCharName> JoinMapByCharName(Guid customerGUID, string characterName, string zoneName, int playerGroupType)
        {
            // TODO: Run Cleanup here for now. Later this can get moved to a scheduler to run periodically.
            await CleanUpInstances(customerGUID);

            JoinMapByCharName outputObject = new JoinMapByCharName();

            string serverIp = "";
            string worldServerIp = "";
            int worldServerPort = 0;
            int port = 0;
            int mapInstanceID = 0;
            string mapNameToStart = "";

            using (Connection)
            {
                //Used and reused by multiple queries
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);

                //Lookup the Zone row by zoneName
                parameters.Add("@ZoneName", zoneName);
                Maps outputMap = await Connection.QuerySingleOrDefaultAsync<Maps>(GenericQueries.GetMapByZoneName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputMap == null)
                {
                    //Error finding Zone: zoneName
                    return new JoinMapByCharName()
                    {
                        Success = false,
                        ErrorMessage = $"Error finding Zone: {zoneName}",
                        ServerIP = serverIp,
                        Port = port,
                        WorldServerID = -1,
                        WorldServerIP = worldServerIp,
                        WorldServerPort = worldServerPort,
                        MapInstanceID = mapInstanceID,
                        MapNameToStart = mapNameToStart,
                        MapInstanceStatus = -1,
                        NeedToStartupMap = false
                    };
                }

                //Lookup the Character row by characterName
                parameters.Add("@CharName", characterName);
                Characters outputCharacter = await Connection.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterByName,
                    parameters,
                    commandType: CommandType.Text);

                /*
                Customers outputCustomer = await Connection.QuerySingleOrDefaultAsync<Customers>(GenericQueries.GetCustomer,
                    parameters,
                    commandType: CommandType.Text);
                */

                //We could not find a valid character for characterName in this customerGUID
                if (outputCharacter == null)
                {
                    return new JoinMapByCharName()
                    {
                        Success = false,
                        ErrorMessage = $"Error finding Character: {characterName}",
                        ServerIP = serverIp,
                        Port = port,
                        WorldServerID = -1,
                        WorldServerIP = worldServerIp,
                        WorldServerPort = worldServerPort,
                        MapInstanceID = mapInstanceID,
                        MapNameToStart = mapNameToStart,
                        MapInstanceStatus = -1,
                        NeedToStartupMap = false
                    }; ;
                }

                //If there is a playerGroupType, then look up the player group by type.  This assumes that for this playerGroupType, the player can only be in at most one Player Group
                PlayerGroup outputPlayerGroup = new PlayerGroup();
                if (playerGroupType > 0)
                {
                    parameters.Add("@PlayerGroupType", playerGroupType);
                    outputPlayerGroup = await Connection.QuerySingleOrDefaultAsync<PlayerGroup>(GenericQueries.GetPlayerGroupIDByType,
                        parameters,
                        commandType: CommandType.Text);
                }
                else
                {
                    outputPlayerGroup.PlayerGroupId = 0;
                }

                //This query has the conditions required to find a Zone Instance to connect the player to
                parameters.Add("@IsInternalNetworkTestUser", outputCharacter.IsInternalNetworkTestUser);
                parameters.Add("@SoftPlayerCap", outputMap.SoftPlayerCap);
                parameters.Add("@PlayerGroupID", outputPlayerGroup.PlayerGroupId);
                parameters.Add("@MapID", outputMap.MapId);
                JoinMapByCharName outputJoinMapByCharName = await Connection.QuerySingleOrDefaultAsync<JoinMapByCharName>(MSSQLQueries.
                    GetZoneInstancesByZoneAndGroup,
                    parameters,
                    commandType: CommandType.Text);

                //We found a Zone Instance to connect the player to
                if (outputJoinMapByCharName != null)
                {
                    outputObject.NeedToStartupMap = false; //false means that the OWS Instance Launcher will NOT be called to spin up a new Zone Instance
                    outputObject.WorldServerID = outputJoinMapByCharName.WorldServerID;
                    outputObject.ServerIP = outputJoinMapByCharName.ServerIP;

                    //If the login username has @localhost in it or if Users.IsInternalNetworkTestUser is set to true, then redirect the client IP to the InternalServerIP (usually 127.0.0.1 on a development PC)
                    //This is useful if you want to play a game client on the same device (development PC) as the game server while still allowing players from outside the network to connect with an external IP.
                    if (outputCharacter.Email.Contains("@localhost") || outputCharacter.IsInternalNetworkTestUser)
                    {
                        outputObject.ServerIP = outputJoinMapByCharName.WorldServerIP;
                    }
                    outputObject.WorldServerIP = outputJoinMapByCharName.WorldServerIP;
                    outputObject.WorldServerPort = outputJoinMapByCharName.WorldServerPort;
                    outputObject.Port = outputJoinMapByCharName.Port;
                    outputObject.MapInstanceID = outputJoinMapByCharName.MapInstanceID;
                    outputObject.MapNameToStart = outputMap.MapName;
                }
                else
                {
                    //We don't have a Zone Instance for the player to connect to, so spin up a new one
                    MapInstances outputMapInstance = await SpinUpInstance(customerGUID, zoneName, outputPlayerGroup.PlayerGroupId);

                    //Get the World Server row by WorldServerID
                    parameters.Add("@WorldServerId", outputMapInstance.WorldServerId);
                    WorldServers outputWorldServers = await Connection.QuerySingleOrDefaultAsync<WorldServers>(GenericQueries.GetWorldByID,
                        parameters,
                        commandType: CommandType.Text);

                    outputObject.NeedToStartupMap = true; //true means that the OWS Instance Launcher will be called to spin up a new Zone Instance
                    outputObject.WorldServerID = outputMapInstance.WorldServerId;
                    outputObject.ServerIP = outputWorldServers.ServerIp;

                    //If the login username has @localhost in it or if Users.IsInternalNetworkTestUser is set to true, then redirect the client IP to the InternalServerIP (usually 127.0.0.1 on a development PC)
                    //This is useful if you want to play a game client on the same device (development PC) as the game server while still allowing players from outside the network to connect with an external IP.
                    if (outputCharacter.Email.Contains("@localhost") || outputCharacter.IsInternalNetworkTestUser)
                    {
                        outputObject.ServerIP = outputWorldServers.InternalServerIp;
                    }
                    outputObject.WorldServerIP = outputWorldServers.InternalServerIp;
                    outputObject.WorldServerPort = outputWorldServers.Port;
                    outputObject.Port = outputMapInstance.Port;
                    outputObject.MapInstanceID = outputMapInstance.MapInstanceId;
                    outputObject.MapNameToStart = outputMap.MapName;
                }
            }

            return outputObject;
        }
        public async Task<MapInstances> SpinUpInstance(Guid customerGUID, string zoneName, int playerGroupId = 0)
        {
            using (Connection)
            {
                //Used and reused by multiple queries
                var parameters = new DynamicParameters();

                //Finds the Active World Server with the least number of Zone Instances running on it
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@ZoneName", zoneName);
                parameters.Add("@PlayerGroupId", playerGroupId);
                //Dead code (Postgres is the only live backend), but GenericQueries.GetActiveWorldServersByLoad
                //now filters on region and Dapper would throw on the missing parameter. No player context here.
                parameters.Add("@ServerRegion", ServerRegions.Default);
                List<WorldServers> outputWorldServers = (List<WorldServers>)await Connection.QueryAsync<WorldServers>(GenericQueries.GetActiveWorldServersByLoad,
                    parameters,
                    commandType: CommandType.Text);

                //If there are any Active World Servers
                if (outputWorldServers.Any())
                {
                    int? firstAvailable = null;
                    foreach (var worldServer in outputWorldServers)
                    {
                        //Get a list of the ports in use by Zone Instances running on the specified WorldServerID
                        parameters.Add("@WorldServerID", worldServer.WorldServerId);
                        var portsInUse = await Connection.QueryAsync<int>(GenericQueries.GetPortsInUseByWorldServer,
                            parameters,
                            commandType: CommandType.Text);

                        //Find the first port number between StartingMapInstancePort and (StartingMapInstancePort + MaxNumberOfInstances) while ignoring any portsInUse
                        firstAvailable = Enumerable.Range(worldServer.StartingMapInstancePort, worldServer.StartingMapInstancePort + worldServer.MaxNumberOfInstances)
                            .Except(portsInUse)
                            .FirstOrDefault();

                        //If the firstAvailable port is valid (greater than StartingMapInstancePort)
                        if (firstAvailable >= worldServer.StartingMapInstancePort)
                        {
                            //Lookup the Zone row based on the ZoneName
                            Maps outputMaps = await Connection.QuerySingleOrDefaultAsync<Maps>(GenericQueries.GetMapByZoneName,
                                parameters,
                                commandType: CommandType.Text);

                            //Add the new Zone Instance row for the instance server we are spinning up
                            parameters.Add("@MapID", outputMaps.MapId);
                            parameters.Add("@Port", firstAvailable);
                            int outputMapInstanceID = await Connection.QuerySingleOrDefaultAsync<int>(MSSQLQueries.AddMapInstance,
                                parameters,
                                commandType: CommandType.Text);

                            //Return the inserted Zone Instance row
                            MapInstances outputMapInstances = new MapInstances() { 
                                CustomerGuid = customerGUID,
                                MapId = outputMaps.MapId,
                                MapInstanceId = outputMapInstanceID,
                                WorldServerId = worldServer.WorldServerId,
                                Port = firstAvailable ?? 0,
                                Status = 1, //Starting up status
                                PlayerGroupId = playerGroupId,
                                NumberOfReportedPlayers = 0
                            };

                            return outputMapInstances;
                        }
                    }

                    //No available ports
                }
            }

            //No active World Servers were found

            //Log the error

            return new MapInstances { MapInstanceId = -1 };
        }
        // MSSQL is not a deployment target (Postgres only), so the zone-ownership guard is not
        // implemented here. The parameter is accepted and ignored rather than refused: refusing would
        // block every save instead of only the stale ones.
        public async Task<bool> UpdateCharacterStats(Guid customerGUID, string characterName, IEnumerable<UpdateCharacterStats> updateCharacterStats, int? callerZoneInstanceId = null)
        {
            using (Connection)
            {
                var statIds = updateCharacterStats.Select(s => s.StatIdentifier).ToArray();
                var statVals = updateCharacterStats.Select(s => s.Value).ToArray();

                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);
                p.Add("@StatIdentifiers", statIds);
                p.Add("@StatValues", statVals);

                await Connection.ExecuteAsync(GenericQueries.UpsertManyCharacterStats, p, commandType: CommandType.Text);
                return true;
            }
        }
        public async Task<bool> UpdateCharacterQuests(Guid customerGUID, string characterName, IEnumerable<UpdateCharacterQuest> updateCharacterQuests, int? callerZoneInstanceId = null)
        {
            using (Connection)
            {
                foreach (UpdateCharacterQuest Quest in updateCharacterQuests)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@CharName", characterName);
                    p.Add("@QuestIDTag", Quest.QuestIDTag);
                    p.Add("@QuestJournalTagContainer", Quest.QuestJournalTagContainer);
                    p.Add("@CustomData", Quest.CustomData);


                    await Connection.ExecuteAsync(GenericQueries.UpdateCharacterQuest, p);
                }

                return true;
            }
        }
        // Durability half of the Postgres fix: the DELETE-then-INSERT rewrite runs in one
        // transaction so a failure mid-loop cannot leave a truncated bag. The economy-revision
        // protocol is Postgres-only (Characters.EconomyRevision ships in the PlayerShops migration),
        // so expectedRevision is rejected here rather than silently ignored — a caller that thinks
        // it has optimistic protection must not be told the write succeeded without it.
        public async Task<UpdateCharacterInventoryResponse> UpdateCharacterInventory(Guid customerGUID, string characterName,
            IEnumerable<UpdateCharacterInventory> updateCharacterInventory, long? expectedRevision = null)
        {
            if (expectedRevision.HasValue)
            {
                return new UpdateCharacterInventoryResponse
                {
                    Success = false,
                    ErrorMessage = "revision_unsupported",
                    ReasonCode = "revision_unsupported"
                };
            }

            using IDbConnection conn = Connection;
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                IEnumerable<int> inventoryIDs = await conn.QueryAsync<int>(GenericQueries.GetCharInventoryIDByCharName,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.Text);

                if (!inventoryIDs.Any())
                {
                    transaction.Rollback();
                    return new UpdateCharacterInventoryResponse
                    {
                        Success = false,
                        ErrorMessage = "no_inventory",
                        ReasonCode = "no_inventory"
                    };
                }

                int charInventoryID = inventoryIDs.First();
                parameters.Add("@CharInventoryID", charInventoryID);

                await conn.ExecuteAsync(GenericQueries.DeleteCharacterInventoryItems,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.Text);

                foreach (UpdateCharacterInventory inventoryItem in updateCharacterInventory ?? Enumerable.Empty<UpdateCharacterInventory>())
                {
                    await conn.ExecuteAsync(GenericQueries.UpdateCharacterInventory,
                        new
                        {
                            CustomerGUID = customerGUID,
                            CharInventoryID = charInventoryID,
                            inventoryItem.ItemIDTag,
                            inventoryItem.Quantity,
                            inventoryItem.InSlotNumber,
                            inventoryItem.CustomData
                        },
                        transaction: transaction,
                        commandType: CommandType.Text);
                }

                transaction.Commit();
                return new UpdateCharacterInventoryResponse { Success = true, ErrorMessage = "", ReasonCode = "" };
            }
            catch
            {
                try { transaction.Rollback(); } catch { /* connection already gone */ }
                throw;
            }
        }
        public async Task<UpdateCharacterCurrencyResponse> UpdateCharacterCurrency(Guid customerGUID, string characterName, int gold, long? expectedRevision = null)
        {
            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);
                parameters.Add("@Gold", gold);

                await Connection.ExecuteAsync(GenericQueries.UpdateCharacterCurrency,
                    parameters,
                    commandType: CommandType.Text);

                // MSSQL has no EconomyRevision protocol (player shops are Postgres-only), so there is
                // nothing to check expectedRevision against; 0 = "no revision", matching the contract.
                return new UpdateCharacterCurrencyResponse { Success = true, NewEconomyRevision = 0 };
            }
        }
        public async Task UpdateCharacterClass(Guid customerGUID, string characterName, string className)
        {
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

                await Connection.ExecuteAsync(MSSQLQueries.UpdateUserLastAccess,
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

                if (outputCharacter.CharacterId > 0)
                {
                    parameters.Add("@CharacterID", outputCharacter.CharacterId);

                    await Connection.ExecuteAsync(GenericQueries.RemoveCharacterFromAllInstances,
                        parameters,
                        commandType: CommandType.Text);
                }
            }
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

                    await Connection.ExecuteAsync(MSSQLQueries.AddQuestToDatabase, parameters);
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
        public async Task<PartyToSend> CreatePartyOrAddMember(Guid customerGUID, PartyToSend partyRequest)
        {
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);

                    if (partyRequest.PartyAction == PartyAction.MessageTypeCreate)
                    {
                        partyRequest.PartyInfo = await Connection.QuerySingleAsync<PartyInfo>("AddNewParty",
                        p,
                        commandType: CommandType.StoredProcedure);
                    }
                   
                    p.Add("@PartyGuid", Guid.Parse(partyRequest.PartyInfo.PartyGuid));
                        
                    IEnumerable<PartyMemberInfo> PartyMembersToAdd = partyRequest.PartyMembers.Clone();
                    PartyMemberInfo LastPartyMember = partyRequest.PartyMembers.LastOrDefault();
                    partyRequest.PartyMembers.Clear();

                    foreach (PartyMemberInfo Party in PartyMembersToAdd)
                    {
                        p.Add("@CharacterName", Party.CharName);
                        p.Add("@CharacterGUID", Guid.Parse(Party.CharGuid));
                        p.Add("@PartyLeader", Party.PartyLeader);

                        if (Party.Equals(LastPartyMember))
                        {
                            partyRequest.PartyMembers.Add(await Connection.QueryAsync<PartyMemberInfo>("AddNewPartyMember",
                            p,
                            commandType: CommandType.StoredProcedure));
                            break;
                        }

                        await Connection.QueryAsync<PartyMemberInfo>("AddNewPartyMember",
                            p,
                            commandType: CommandType.StoredProcedure);
                    }

                }
                
            }
            catch (Exception ex)
            {
            }
            return partyRequest;
        }
        public async Task<PartyToSend> GetInitialPartySettings(Guid customerGUID, string charName)
        {
            PartyToSend partyRequest = new PartyToSend();
            partyRequest.PartyAction = PartyAction.MessageTypeCreate;
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@CharName", charName);

                    int partyId = await Connection.QuerySingleAsync<int>(GenericQueries.GetPartyId,
                    p,
                    commandType: CommandType.Text);

                    p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@PartyID", partyId);

                    partyRequest.PartyMembers.Add(await Connection.QueryAsync<PartyMemberInfo>("GetInitialPartyMembers",
                                                p,
                                                commandType: CommandType.StoredProcedure));

                    partyRequest.PartyInfo = await Connection.QuerySingleAsync<PartyInfo>("GetInitialPartySettings",
                    p,
                    commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
            }
            return partyRequest;
        }
        public async Task<PartyToSend> LeaveParty(Guid customerGUID, PartyToSend partyRequest)
        {
            PartyToSend party = partyRequest.Clone();
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@PartyGuid", Guid.Parse(partyRequest.PartyInfo.PartyGuid));
                    foreach (PartyMemberInfo partyMember in partyRequest.PartyMembers)
                    {
                        p.Add("@CharName", partyMember.CharName);
                        p.Add("@PartyLeader", partyMember.PartyLeader);

                        party.PartyMembers.Clear();
                        party.PartyMembers.Add(await Connection.QueryAsync<PartyMemberInfo>("LeaveParty",
                                                    p,
                                                    commandType: CommandType.StoredProcedure));
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return party;
        }
        public async Task<PartyToSend> ChangePartyLeader(Guid customerGUID, PartyToSend partyRequest)
        {
            PartyToSend party = partyRequest.Clone();
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@PartyGuid", Guid.Parse(partyRequest.PartyInfo.PartyGuid));
                    PartyMemberInfo partyMember = partyRequest.PartyMembers.ElementAt(0);
                    if(partyMember != null && partyMember != default) 
                    {
                        p.Add("@CharName", partyMember.CharName);
                        
                        party.PartyMembers.Clear();
                        party.PartyMembers.Add(await Connection.QueryAsync<PartyMemberInfo>("ChangePartyLeader",
                                                    p,
                                                    commandType: CommandType.StoredProcedure));
                    }
                    
                }
            }
            catch (Exception ex)
            {
            }
            return party;
        }

        public async Task<bool> IsPartyNameAvailable(Guid customerGUID, string partyName)
        {
            string normalizedPartyName = PartyNameGenerator.Normalize(partyName);
            if (!PartyNameGenerator.IsValid(normalizedPartyName))
            {
                return false;
            }

            using IDbConnection connection = Connection;
            int existingNameCount = await connection.QuerySingleAsync<int>(
                @"SELECT COUNT(*)
                FROM Party
                WHERE CustomerGUID = @CustomerGUID
                    AND LOWER(PartyName) = LOWER(@PartyName)
                    AND DisbandedAt IS NULL",
                new { CustomerGUID = customerGUID, PartyName = normalizedPartyName },
                commandType: CommandType.Text);

            return existingNameCount == 0;
        }

        public async Task<string> GenerateAvailablePartyName(Guid customerGUID)
        {
            for (int attempt = 0; attempt < 40; attempt++)
            {
                string candidate = PartyNameGenerator.CreateCandidate(attempt);
                if (await IsPartyNameAvailable(customerGUID, candidate))
                {
                    return candidate;
                }
            }

            string fallback = PartyNameGenerator.CreateFallback();
            if (await IsPartyNameAvailable(customerGUID, fallback))
            {
                return fallback;
            }

            throw new InvalidOperationException("Unable to generate a unique party name.");
        }

        public Task<PartyToSend> UpdatePartyName(Guid customerGUID, Guid partyGuid, string actorCharName, string actorCharGuid, string partyName)
        {
            throw new NotSupportedException("Party name updates require the Phase 2 party model for this DB backend.");
        }

        public Task<PartyToSend> UpdatePartyDescription(Guid customerGUID, Guid partyGuid, string actorCharName, string actorCharGuid, string partyDescription)
        {
            throw new NotSupportedException("Party description updates require the Phase 2 party model for this DB backend.");
        }

        public Task<PartyToSend> UpdatePartyExpDistribution(Guid customerGUID, Guid partyGuid, string actorCharName, string actorCharGuid, int expDistributionMode)
        {
            throw new NotSupportedException("Party exp distribution updates require the Phase 2 party model for this DB backend.");
        }

        public Task<PartyToSend> UpdatePartyLootDistribution(Guid customerGUID, Guid partyGuid, string actorCharName, string actorCharGuid, int lootDistributionMode)
        {
            throw new NotSupportedException("Party loot distribution updates require the Phase 2 party model for this DB backend.");
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

        public Task UpdateCharacterAbilities(Guid customerGUID, string characterName, string characterAbilities)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CharacterAbilityDto>> GetCharacterAbilities(Guid customerGUID, string characterName)
        {
            throw new NotImplementedException();
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
