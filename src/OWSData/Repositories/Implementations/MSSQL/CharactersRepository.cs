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
using OWSShared.Options;
using System.Net;

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
                    return default(MapInstances);
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
            catch
            {
                transaction.Rollback();
                throw new Exception("Database Exception in CleanUpInstances!");
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
            // TODO: Run Cleanup here for now. Later this can get moved to a scheduler to run periodically.
            await CleanUpInstances(customerGUID);

            JoinMapByCharName outputObject = default;

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
                    return default(JoinMapByCharName) with
                    {
                        Success = false,
                        ErrorMessage = $"Error finding Zone: {zoneName}",
                        ServerIP = IPAddress.Parse(serverIp),
                        Port = port,
                        WorldServerID = -1,
                        WorldServerIP = IPAddress.Parse(worldServerIp),
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
                    return default(JoinMapByCharName) with
                    {
                        Success = false,
                        ErrorMessage = $"Error finding Character: {characterName}",
                        ServerIP = IPAddress.Parse(serverIp),
                        Port = port,
                        WorldServerID = -1,
                        WorldServerIP = IPAddress.Parse(worldServerIp),
                        WorldServerPort = worldServerPort,
                        MapInstanceID = mapInstanceID,
                        MapNameToStart = mapNameToStart,
                        MapInstanceStatus = -1,
                        NeedToStartupMap = false
                    }; ;
                }

                //If there is a playerGroupType, then look up the player group by type.  This assumes that for this playerGroupType, the player can only be in at most one Player Group
                PlayerGroup outputPlayerGroup = default(PlayerGroup);
                if (playerGroupType > 0)
                {
                    parameters.Add("@PlayerGroupType", playerGroupType);
                    outputPlayerGroup = await Connection.QuerySingleOrDefaultAsync<PlayerGroup>(GenericQueries.GetPlayerGroupIDByType,
                        parameters,
                        commandType: CommandType.Text);
                }
                else
                {
                    outputPlayerGroup = outputPlayerGroup with { PlayerGroupId = 0 };
                }

                //This query has the conditions required to find a Zone Instance to connect the player to
                parameters.Add("@IsInternalNetworkTestUser", outputCharacter.IsInternalNetworkTestUser);
                parameters.Add("@SoftPlayerCap", outputMap.SoftPlayerCap);
                parameters.Add("@PlayerGroupID", outputPlayerGroup.PlayerGroupId);
                parameters.Add("@MapID", outputMap.MapId);
                JoinMapByCharName outputJoinMapByCharName = await Connection.QuerySingleOrDefaultAsync<JoinMapByCharName>(MSSQLQueries.GetZoneInstancesByZoneAndGroup,
                    parameters,
                    commandType: CommandType.Text);

                //We found a Zone Instance to connect the player to
                if (outputJoinMapByCharName != null)
                {
                    outputObject = outputObject with
                    {
                        NeedToStartupMap = false, //false means that the OWS Instance Launcher will NOT be called to spin up a new Zone Instance
                        WorldServerID = outputJoinMapByCharName.WorldServerID,
                        ServerIP = outputJoinMapByCharName.ServerIP
                    };

                    //If the login username has @localhost in it or if Users.IsInternalNetworkTestUser is set to true, then redirect the client IP to the InternalServerIP (usually 127.0.0.1 on a development PC)
                    //This is useful if you want to play a game client on the same device (development PC) as the game server while still allowing players from outside the network to connect with an external IP.
                    if (outputCharacter.Email.Contains("@localhost") || outputCharacter.IsInternalNetworkTestUser)
                    {
                        outputObject = outputObject with { ServerIP = outputJoinMapByCharName.WorldServerIP };
                    }

                    outputObject = outputObject with
                    {
                        WorldServerIP = outputJoinMapByCharName.WorldServerIP,
                        WorldServerPort = outputJoinMapByCharName.WorldServerPort,
                        Port = outputJoinMapByCharName.Port,
                        MapInstanceID = outputJoinMapByCharName.MapInstanceID,
                        MapNameToStart = outputMap.MapName
                    };
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

                    outputObject = outputObject with
                    {
                        NeedToStartupMap = true, //true means that the OWS Instance Launcher will be called to spin up a new Zone Instance
                        WorldServerID = outputMapInstance.WorldServerId,
                        ServerIP = outputWorldServers.ServerIp,
                        WorldServerIP = outputWorldServers.InternalServerIp,
                        WorldServerPort = outputWorldServers.Port,
                        Port = outputMapInstance.Port,
                        MapInstanceID = outputMapInstance.MapInstanceId,
                        MapNameToStart = outputMap.MapName
                    };


                    //If the login username has @localhost in it or if Users.IsInternalNetworkTestUser is set to true, then redirect the client IP to the InternalServerIP (usually 127.0.0.1 on a development PC)
                    //This is useful if you want to play a game client on the same device (development PC) as the game server while still allowing players from outside the network to connect with an external IP.
                    if (outputCharacter.Email.Contains("@localhost") || outputCharacter.IsInternalNetworkTestUser)
                    {
                        outputObject = outputObject with { ServerIP = outputWorldServers.InternalServerIp };
                    }
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

                            //Get the recently inserted Zone Instance row
                            /*
                            parameters.Add("@MapInstanceID", outputMapInstanceID);
                            MapInstances outputMapInstances = await Connection.QuerySingleOrDefaultAsync<MapInstances>(GenericQueries.GetMapInstance,
                                parameters,
                                commandType: CommandType.Text);
                            */

                            //Return the inserted Zone Instance row
                            MapInstances outputMapInstances = default(MapInstances) with
                            {
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

            return default(MapInstances) with { MapInstanceId = -1 };
        }

        public async Task UpdateCharacterStats(UpdateCharacterStats updateCharacterStats)
        {
            using (Connection)
            {
                await Connection.ExecuteAsync(GenericQueries.UpdateCharacterStats,
                    updateCharacterStats,
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
                    await Connection.ExecuteAsync(MSSQLQueries.AddAbilityToCharacter,
                        parameters,
                        commandType: CommandType.Text);
                }
            }
        }

        public async Task<IEnumerable<Abilities>> GetAbilities(Guid customerGUID)
        {
            IEnumerable<Abilities> outputGetAbilities;

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);

                outputGetAbilities = await Connection.QueryAsync<Abilities>(GenericQueries.GetAbilities,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputGetAbilities;
        }

        public async Task<IEnumerable<GetCharacterAbilities>> GetCharacterAbilities(Guid customerGUID, string characterName)
        {
            IEnumerable<GetCharacterAbilities> outputGetCharacterAbilities;

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputGetCharacterAbilities = await Connection.QueryAsync<GetCharacterAbilities>(GenericQueries.GetCharacterAbilities,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputGetCharacterAbilities;
        }

        public async Task<IEnumerable<GetAbilityBars>> GetAbilityBars(Guid customerGUID, string characterName)
        {
            IEnumerable<GetAbilityBars> outputGetAbilityBars;

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputGetAbilityBars = await Connection.QueryAsync<GetAbilityBars>(GenericQueries.GetCharacterAbilityBars,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputGetAbilityBars;
        }

        public async Task<IEnumerable<GetAbilityBarsAndAbilities>> GetAbilityBarsAndAbilities(Guid customerGUID, string characterName)
        {
            IEnumerable<GetAbilityBarsAndAbilities> outputGetAbilityBarsAndAbilities;

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputGetAbilityBarsAndAbilities = await Connection.QueryAsync<GetAbilityBarsAndAbilities>(GenericQueries.GetCharacterAbilityBarsAndAbilities,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputGetAbilityBarsAndAbilities;
        }

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

                await Connection.ExecuteAsync(MSSQLQueries.RemoveAbilityFromCharacter, parameters);
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

                await Connection.ExecuteAsync(MSSQLQueries.UpdateAbilityOnCharacter, parameters);
            }
        }
    }
}
