using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
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

namespace OWSData.Repositories.Implementations.Postgres
{
    public class CharactersRepository : ICharactersRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public CharactersRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private IDbConnection Connection => new NpgsqlConnection(_storageOptions.Value.OWSDBConnectionString);

        public async Task AddCharacterToMapInstanceByCharName(Guid customerGUID, string characterName, int mapInstanceID)
        {
            using (var connection = (NpgsqlConnection)Connection)
            {
                await connection.OpenAsync();

                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@CustomerGUID", customerGUID);
                        parameters.Add("@CharName", characterName);
                        parameters.Add("@MapInstanceID", mapInstanceID);

                        var outputCharacter = await connection.QuerySingleOrDefaultAsync<Characters>(
                            GenericQueries.GetCharacterIDByName,
                            parameters,
                            commandType: CommandType.Text);

                        var outputZone = await connection.QuerySingleOrDefaultAsync<Maps>(GenericQueries.GetZoneName,
                            parameters,
                            commandType: CommandType.Text);

                        if (outputCharacter.CharacterId > 0)
                        {
                            parameters.Add("@CharacterID", outputCharacter.CharacterId);
                            parameters.Add("@ZoneName", outputZone.ZoneName);

                            await connection.ExecuteAsync(GenericQueries.RemoveCharacterFromAllInstances,
                                parameters,
                                commandType: CommandType.Text);

                            await connection.ExecuteAsync(GenericQueries.AddCharacterToInstance,
                                parameters,
                                commandType: CommandType.Text);

                            await connection.ExecuteAsync(GenericQueries.UpdateCharacterZone,
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
            }
        }

        public async Task AddOrUpdateCustomCharacterData(Guid customerGUID, AddOrUpdateCustomCharacterData addOrUpdateCustomCharacterData)
        {
            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", addOrUpdateCustomCharacterData.CharacterName);
                parameters.Add("@CustomFieldName", addOrUpdateCustomCharacterData.CustomFieldName);
                parameters.Add("@FieldValue", addOrUpdateCustomCharacterData.FieldValue);

                var outputCharacter = await connection.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterIDByName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputCharacter.CharacterId > 0)
                {
                    parameters.Add("@CharacterID", outputCharacter.CharacterId);

                    var hasCustomCharacterData = await connection.QuerySingleOrDefaultAsync<int>(GenericQueries.HasCustomCharacterDataForField,
                        parameters,
                        commandType: CommandType.Text);

                    if (hasCustomCharacterData > 0)
                    {
                        await connection.ExecuteAsync(GenericQueries.UpdateCharacterCustomDataField,
                            parameters,
                            commandType: CommandType.Text);
                    }
                    else
                    {
                        await connection.ExecuteAsync(GenericQueries.AddCharacterCustomDataField,
                            parameters,
                            commandType: CommandType.Text);
                    }
                }
            }
        }

        public async Task<MapInstances> CheckMapInstanceStatus(Guid customerGUID, int mapInstanceID)
        {
            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@MapInstanceID", mapInstanceID);

                var outputObject = await connection.QuerySingleOrDefaultAsync<MapInstances>(GenericQueries.GetMapInstanceStatus,
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
            using (var connection = (NpgsqlConnection)Connection)
            {
                await connection.OpenAsync();

                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@CustomerGUID", customerGUID);
                        parameters.Add("@CharacterMinutes", 1); // TODO Add Configuration Parameter
                        parameters.Add("@MapMinutes", 2); // TODO Add Configuration Parameter

                        await transaction.ExecuteAsync(PostgresQueries.RemoveCharactersFromAllInactiveInstances,
                            parameters,
                            commandType: CommandType.Text);

                        var outputMapInstances = await transaction.QueryAsync<int>(
                            PostgresQueries.GetAllInactiveMapInstances,
                            parameters,
                            commandType: CommandType.Text);

                        if (outputMapInstances.Any())
                        {
                            parameters.Add("@MapInstances", outputMapInstances);

                            await transaction.ExecuteAsync(PostgresQueries.RemoveCharacterFromInstances,
                                parameters,
                                commandType: CommandType.Text);

                            await transaction.ExecuteAsync(PostgresQueries.RemoveMapInstances,
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
            }
        }

        public async Task<GetCharByCharName> GetCharByCharName(Guid customerGUID, string characterName)
        {
            IEnumerable<GetCharByCharName> outputCharacter;

            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputCharacter = await connection.QueryAsync<GetCharByCharName>(GenericQueries.GetCharByCharName,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputCharacter.FirstOrDefault();
        }

        public async Task<IEnumerable<CustomCharacterData>> GetCustomCharacterData(Guid customerGUID, string characterName)
        {
            IEnumerable<CustomCharacterData> outputCustomCharacterDataRows;

            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputCustomCharacterDataRows = await connection.QueryAsync<CustomCharacterData>(GenericQueries.GetCharacterCustomDataByName,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputCustomCharacterDataRows;
        }
        public async Task<IEnumerable<DefaultCustomData>> GetDefaultCustomCharacterData(Guid customerGUID, string defaultSetName)
        {
            IEnumerable<DefaultCustomData> outputDefaultCustomCharacterDataRows;

            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@DefaultSetName", defaultSetName);

                outputDefaultCustomCharacterDataRows = await connection.QueryAsync<DefaultCustomData>(GenericQueries.GetDefaultCustomCharacterDataByDefaultSetName,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputDefaultCustomCharacterDataRows;
        }

        public async Task<JoinMapByCharName> JoinMapByCharName(Guid customerGUID, string characterName, string zoneName, int playerGroupType)
        {
            // TODO: Run Cleanup here for now. Later this can get moved to a scheduler to run periodically.
            await CleanUpInstances(customerGUID);

            JoinMapByCharName outputObject = new JoinMapByCharName();

            string serverIp = "";
            int? worldServerId = 0;
            string worldServerIp = "";
            int worldServerPort = 0;
            int port = 0;
            int mapInstanceID = 0;
            string mapNameToStart = "";
            int? mapInstanceStatus = 0;
            bool needToStartupMap = false;
            bool enableAutoLoopback = false;
            bool noPortForwarding = false;

            outputObject = new JoinMapByCharName()
            {
                ServerIP = serverIp,
                Port = port,
                WorldServerID = -1,
                WorldServerIP = worldServerIp,
                WorldServerPort = worldServerPort,
                MapInstanceID = mapInstanceID,
                MapNameToStart = mapNameToStart,
                MapInstanceStatus = -1,
                NeedToStartupMap = false,
                EnableAutoLoopback = false,
                NoPortForwarding = false
            };

            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);
                parameters.Add("@ZoneName", zoneName);
                parameters.Add("@PlayerGroupType", playerGroupType);

                Maps outputMap = await connection.QuerySingleOrDefaultAsync<Maps>(GenericQueries.GetMapByZoneName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputMap == null)
                {
                    Console.WriteLine($"CharactersRepository: JoinMapByCharName - Unable to find Zone Name: {zoneName} for CustomerGUID: {customerGUID}  Check your Maps table for this row!");

                    return outputObject;
                }

                Characters outputCharacter = await connection.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterByName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputCharacter == null)
                {
                    Console.WriteLine($"CharactersRepository: JoinMapByCharName - Unable to find Character by Name: {characterName} for CustomerGUID: {customerGUID}");

                    return outputObject;
                }

                Customers outputCustomer = await connection.QuerySingleOrDefaultAsync<Customers>(GenericQueries.GetCustomer,
                    parameters,
                    commandType: CommandType.Text);

                if (outputCustomer == null)
                {
                    Console.WriteLine($"CharactersRepository: JoinMapByCharName - Unable to find Customer for CustomerGUID: {customerGUID}");

                    return outputObject;
                }

                PlayerGroup outputPlayerGroup = new PlayerGroup();

                if (playerGroupType > 0)
                {
                    outputPlayerGroup = await connection.QuerySingleOrDefaultAsync<PlayerGroup>(GenericQueries.GetPlayerGroupIDByType,
                        parameters,
                        commandType: CommandType.Text);
                }
                else
                {
                    outputPlayerGroup.PlayerGroupId = 0;
                }

                parameters.Add("@IsInternalNetworkTestUser", outputCharacter.IsInternalNetworkTestUser);
                parameters.Add("@SoftPlayerCap", outputMap.SoftPlayerCap);
                parameters.Add("@PlayerGroupID", outputPlayerGroup.PlayerGroupId);
                parameters.Add("@MapID", outputMap.MapId);

                JoinMapByCharName outputJoinMapByCharName = await connection.QuerySingleOrDefaultAsync<JoinMapByCharName>(PostgresQueries.GetZoneInstancesByZoneAndGroup,
                    parameters,
                    commandType: CommandType.Text);

                if (outputJoinMapByCharName != null)
                {
                    outputObject.NeedToStartupMap = false;
                    outputObject.WorldServerID = outputJoinMapByCharName.WorldServerID;
                    outputObject.ServerIP = outputJoinMapByCharName.ServerIP;
                    if (outputCharacter.IsInternalNetworkTestUser)
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
                    MapInstances outputMapInstance = await SpinUpInstance(customerGUID, zoneName, outputPlayerGroup.PlayerGroupId);

                    parameters.Add("@WorldServerId", outputMapInstance.WorldServerId);

                    WorldServers outputWorldServers =  await connection.QuerySingleOrDefaultAsync<WorldServers>(GenericQueries.GetWorldByID,
                        parameters,
                        commandType: CommandType.Text);

                    outputObject.NeedToStartupMap = true;
                    outputObject.WorldServerID = outputMapInstance.WorldServerId;
                    outputObject.ServerIP = outputWorldServers.ServerIp;
                    if (outputCharacter.IsInternalNetworkTestUser)
                    {
                        outputObject.ServerIP = outputWorldServers.InternalServerIp;
                    }
                    outputObject.WorldServerIP = outputWorldServers.InternalServerIp;
                    outputObject.WorldServerPort = outputWorldServers.Port;
                    outputObject.Port = outputMapInstance.Port;
                    outputObject.MapInstanceID = outputMapInstance.MapInstanceId;
                    outputObject.MapNameToStart = outputMap.MapName;
                }

                if (outputCharacter.Email.Contains("@localhost") || outputCharacter.IsInternalNetworkTestUser)
                {
                    outputObject.ServerIP = "127.0.0.1";
                }

            }

            return outputObject;
        }

        public async Task<MapInstances> SpinUpInstance(Guid customerGUID, string zoneName, int playerGroupId = 0)
        {
            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@ZoneName", zoneName);
                parameters.Add("@PlayerGroupId", playerGroupId);

                List<WorldServers> outputWorldServers = (List<WorldServers>)await connection.QueryAsync<WorldServers>(GenericQueries.GetActiveWorldServersByLoad,
                    parameters,
                    commandType: CommandType.Text);

                if (outputWorldServers.Any())
                {
                    foreach (var worldServer in outputWorldServers)
                    {
                        var portsInUse = await connection.QueryAsync<int>(GenericQueries.GetPortsInUseByWorldServer,
                            parameters,
                            commandType: CommandType.Text);

                        int? firstAvailable = Enumerable.Range(worldServer.StartingMapInstancePort, worldServer.StartingMapInstancePort + worldServer.MaxNumberOfInstances)
                            .Except(portsInUse)
                            .FirstOrDefault();

                        if (firstAvailable >= worldServer.StartingMapInstancePort)
                        {

                            Maps outputMaps = await connection.QuerySingleOrDefaultAsync<Maps>(GenericQueries.GetMapByZoneName,
                                parameters,
                                commandType: CommandType.Text);

                            parameters.Add("@WorldServerID", worldServer.WorldServerId);
                            parameters.Add("@MapID", outputMaps.MapId);
                            parameters.Add("@Port", firstAvailable);

                            int outputMapInstanceId = await connection.QuerySingleOrDefaultAsync<int>(PostgresQueries.AddMapInstance,
                                parameters,
                                commandType: CommandType.Text);

                            parameters.Add("@MapInstanceID", outputMapInstanceId);

                            MapInstances outputMapInstances = await connection.QuerySingleOrDefaultAsync<MapInstances>(GenericQueries.GetMapInstance,
                                parameters,
                                commandType: CommandType.Text);

                            return outputMapInstances;
                        }
                    }
                }
            }

            return new MapInstances { MapInstanceId = -1 };
        }

        public async Task UpdateCharacterStats(UpdateCharacterStats updateCharacterStats)
        {
            using (var connection = (NpgsqlConnection)Connection)
            {
                await connection.ExecuteAsync(GenericQueries.UpdateCharacterStats.Replace("@CustomerGUID", "@CustomerGUID::uuid"), // NOTE Postgres text=>uuid
                    updateCharacterStats,
                    commandType: CommandType.Text);
            }
        }

        public async Task UpdatePosition(Guid customerGUID, string characterName, string mapName, float X, float Y, float Z, float RX, float RY, float RZ)
        {
            using (var connection = (NpgsqlConnection)Connection)
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
                    await connection.ExecuteAsync(GenericQueries.UpdateCharacterPositionAndMap,
                        p,
                        commandType: CommandType.Text);
                }
                else
                {
                    await connection.ExecuteAsync(GenericQueries.UpdateCharacterPosition,
                        p,
                        commandType: CommandType.Text);
                }

                await connection.ExecuteAsync(PostgresQueries.UpdateUserLastAccess,
                    p,
                    commandType: CommandType.Text);
            }
        }

        public async Task PlayerLogout(Guid customerGUID, string characterName)
        {
            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                var outputCharacter = await connection.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterIDByName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputCharacter.CharacterId > 0)
                {
                    parameters.Add("@CharacterID", outputCharacter.CharacterId);

                    await connection.ExecuteAsync(GenericQueries.RemoveCharacterFromAllInstances,
                        parameters,
                        commandType: CommandType.Text);
                }
            }
        }

        public async Task AddAbilityToCharacter(Guid customerGUID, string abilityName, string characterName, int abilityLevel, string charHasAbilitiesCustomJSON)
        {
            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new
                {
                    CustomerGUID = customerGUID,
                    AbilityName = abilityName,
                    CharacterName = characterName,
                    AbilityLevel = abilityLevel,
                    CharHasAbilitiesCustomJSON = charHasAbilitiesCustomJSON
                };

                var outputCharacterAbility = await connection.QuerySingleOrDefaultAsync<GlobalData>(GenericQueries.GetCharacterAbilityByName,
                    parameters,
                    commandType: CommandType.Text);

                if (outputCharacterAbility == null)
                {
                    await connection.ExecuteAsync(PostgresQueries.AddAbilityToCharacter,
                        parameters,
                        commandType: CommandType.Text);
                }
            }
        }

        public async Task<IEnumerable<Abilities>> GetAbilities(Guid customerGUID)
        {
            IEnumerable<Abilities> outputGetAbilities;

            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);

                outputGetAbilities = await connection.QueryAsync<Abilities>(GenericQueries.GetAbilities,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputGetAbilities;
        }

        public async Task<IEnumerable<GetCharacterAbilities>> GetCharacterAbilities(Guid customerGUID, string characterName)
        {
            IEnumerable<GetCharacterAbilities> outputGetCharacterAbilities;

            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputGetCharacterAbilities = await connection.QueryAsync<GetCharacterAbilities>(GenericQueries.GetCharacterAbilities,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputGetCharacterAbilities;
        }

        public async Task<IEnumerable<GetAbilityBars>> GetAbilityBars(Guid customerGUID, string characterName)
        {
            IEnumerable<GetAbilityBars> outputGetAbilityBars;

            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputGetAbilityBars = await connection.QueryAsync<GetAbilityBars>(GenericQueries.GetCharacterAbilityBars,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputGetAbilityBars;
        }

        public async Task<IEnumerable<GetAbilityBarsAndAbilities>> GetAbilityBarsAndAbilities(Guid customerGUID, string characterName)
        {
            IEnumerable<GetAbilityBarsAndAbilities> outputGetAbilityBarsAndAbilities;

            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);

                outputGetAbilityBarsAndAbilities = await connection.QueryAsync<GetAbilityBarsAndAbilities>(GenericQueries.GetCharacterAbilityBarsAndAbilities,
                    parameters,
                    commandType: CommandType.Text);
            }

            return outputGetAbilityBarsAndAbilities;
        }

        public async Task RemoveAbilityFromCharacter(Guid customerGUID, string abilityName, string characterName)
        {
            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new
                {
                    CustomerGUID = customerGUID,
                    AbilityName = abilityName,
                    CharacterName = characterName
                };

                await connection.ExecuteAsync(PostgresQueries.RemoveAbilityFromCharacter, parameters);
            }
        }

        public async Task UpdateAbilityOnCharacter(Guid customerGUID, string abilityName, string characterName, int abilityLevel, string charHasAbilitiesCustomJSON)
        {
            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new
                {
                    CustomerGUID = customerGUID,
                    AbilityName = abilityName,
                    CharacterName = characterName,
                    AbilityLevel = abilityLevel,
                    CharHasAbilitiesCustomJSON = charHasAbilitiesCustomJSON
                };

                await connection.ExecuteAsync(PostgresQueries.UpdateAbilityOnCharacter, parameters);
            }
        }
    }
}
