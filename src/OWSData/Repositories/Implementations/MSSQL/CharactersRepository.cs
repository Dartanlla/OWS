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

namespace OWSData.Repositories.Implementations.MSSQL
{
    public class CharactersRepository : ICharactersRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public CharactersRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        public IDbConnection Connection
        {
            get
            {
                return new SqlConnection(_storageOptions.Value.OWSDBConnectionString);
            }
        }

        public async Task AddCharacterToMapInstanceByCharName(Guid customerGUID, string characterName, int mapInstanceID)
        {
            // TODO Add Logging

            using (Connection)
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
            }
        }

        public async Task AddOrUpdateCustomCharacterData(Guid customerGUID, AddOrUpdateCustomCharacterData addOrUpdateCustomCharacterData)
        {
            // TODO Add Logging

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
            // TODO Add Logging

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
            // TODO Add Logging

            IDbConnection conn = Connection;
            conn.Open();
            using (IDbTransaction transaction = conn.BeginTransaction())
            {
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
        }

        public async Task<GetCharByCharName> GetCharByCharName(Guid customerGUID, string characterName)
        {
            // TODO Add Logging

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

            return outputCharacter.First();
        }

        public async Task<IEnumerable<CustomCharacterData>> GetCustomCharacterData(Guid customerGUID, string characterName)
        {
            // TODO Add Logging

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
                    outputObject = new JoinMapByCharName() {
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

                    return outputObject;
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
                JoinMapByCharName outputJoinMapByCharName = await Connection.QuerySingleOrDefaultAsync<JoinMapByCharName>(GenericQueries.GetZoneInstancesByZoneAndGroup,
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
                    WorldServers outputWorldServers =  await Connection.QuerySingleOrDefaultAsync<WorldServers>(GenericQueries.GetWorldByID,
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
            // TODO Add Logging

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

        public async Task UpdateCharacterStats(UpdateCharacterStats updateCharacterStats)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", updateCharacterStats.CustomerGUID);
                p.Add("@CharName", updateCharacterStats.CharName);
                p.Add("@CharacterLevel", updateCharacterStats.CharacterLevel);
                p.Add("@Gender", updateCharacterStats.Gender);
                p.Add("@Weight", updateCharacterStats.Weight);
                p.Add("@Size", updateCharacterStats.Size);
                p.Add("@Fame", updateCharacterStats.Fame);
                p.Add("@Alignment", updateCharacterStats.Alignment);
                p.Add("@Description", updateCharacterStats.Description);
                p.Add("@XP", updateCharacterStats.XP);
                p.Add("@X", updateCharacterStats.X);
                p.Add("@Y", updateCharacterStats.Y);
                p.Add("@Z", updateCharacterStats.Z);
                p.Add("@RX", updateCharacterStats.RX);
                p.Add("@RY", updateCharacterStats.RY);
                p.Add("@RZ", updateCharacterStats.RZ);
                p.Add("@TeamNumber", updateCharacterStats.TeamNumber);
                p.Add("@HitDie", updateCharacterStats.HitDie);
                p.Add("@Wounds", updateCharacterStats.Wounds);
                p.Add("@Thirst", updateCharacterStats.Thirst);
                p.Add("@Hunger", updateCharacterStats.Hunger);
                p.Add("@MaxHealth", updateCharacterStats.MaxHealth);
                p.Add("@Health", updateCharacterStats.Health);
                p.Add("@HealthRegenRate", updateCharacterStats.HealthRegenRate);
                p.Add("@MaxMana", updateCharacterStats.MaxMana);
                p.Add("@Mana", updateCharacterStats.Mana);
                p.Add("@ManaRegenRate", updateCharacterStats.ManaRegenRate);
                p.Add("@MaxEnergy", updateCharacterStats.MaxEnergy);
                p.Add("@Energy", updateCharacterStats.Energy);
                p.Add("@EnergyRegenRate", updateCharacterStats.EnergyRegenRate);
                p.Add("@MaxFatigue", updateCharacterStats.MaxFatigue);
                p.Add("@Fatigue", updateCharacterStats.Fatigue);
                p.Add("@FatigueRegenRate", updateCharacterStats.FatigueRegenRate);
                p.Add("@MaxStamina", updateCharacterStats.MaxStamina);
                p.Add("@Stamina", updateCharacterStats.Stamina);
                p.Add("@StaminaRegenRate", updateCharacterStats.StaminaRegenRate);
                p.Add("@MaxEndurance", updateCharacterStats.MaxEndurance);
                p.Add("@Endurance", updateCharacterStats.Endurance);
                p.Add("@EnduranceRegenRate", updateCharacterStats.EnduranceRegenRate);
                p.Add("@Strength", updateCharacterStats.Strength);
                p.Add("@Dexterity", updateCharacterStats.Dexterity);
                p.Add("@Constitution", updateCharacterStats.Constitution);
                p.Add("@Intellect", updateCharacterStats.Intellect);
                p.Add("@Wisdom", updateCharacterStats.Wisdom);
                p.Add("@Charisma", updateCharacterStats.Charisma);
                p.Add("@Agility", updateCharacterStats.Agility);
                p.Add("@Spirit", updateCharacterStats.Spirit);
                p.Add("@Magic", updateCharacterStats.Magic);
                p.Add("@Fortitude", updateCharacterStats.Fortitude);
                p.Add("@Reflex", updateCharacterStats.Reflex);
                p.Add("@Willpower", updateCharacterStats.Willpower);
                p.Add("@BaseAttack", updateCharacterStats.BaseAttack);
                p.Add("@BaseAttackBonus", updateCharacterStats.BaseAttackBonus);
                p.Add("@AttackPower", updateCharacterStats.AttackPower);
                p.Add("@AttackSpeed", updateCharacterStats.AttackSpeed);
                p.Add("@CritChance", updateCharacterStats.CritChance);
                p.Add("@CritMultiplier", updateCharacterStats.CritMultiplier);
                p.Add("@Haste", updateCharacterStats.Haste);
                p.Add("@SpellPower", updateCharacterStats.SpellPower);
                p.Add("@SpellPenetration", updateCharacterStats.SpellPenetration);
                p.Add("@Defense", updateCharacterStats.Defense);
                p.Add("@Dodge", updateCharacterStats.Dodge);
                p.Add("@Parry", updateCharacterStats.Parry);
                p.Add("@Avoidance", updateCharacterStats.Avoidance);
                p.Add("@Versatility", updateCharacterStats.Versatility);
                p.Add("@Multishot", updateCharacterStats.Multishot);
                p.Add("@Initiative", updateCharacterStats.Initiative);
                p.Add("@NaturalArmor", updateCharacterStats.NaturalArmor);
                p.Add("@PhysicalArmor", updateCharacterStats.PhysicalArmor);
                p.Add("@BonusArmor", updateCharacterStats.BonusArmor);
                p.Add("@ForceArmor", updateCharacterStats.ForceArmor);
                p.Add("@MagicArmor", updateCharacterStats.MagicArmor);
                p.Add("@Resistance", updateCharacterStats.Resistance);
                p.Add("@ReloadSpeed", updateCharacterStats.ReloadSpeed);
                p.Add("@Range", updateCharacterStats.Range);
                p.Add("@Speed", updateCharacterStats.Speed);
                p.Add("@Gold", updateCharacterStats.Gold);
                p.Add("@Silver", updateCharacterStats.Silver);
                p.Add("@Copper", updateCharacterStats.Copper);
                p.Add("@FreeCurrency", updateCharacterStats.FreeCurrency);
                p.Add("@PremiumCurrency", updateCharacterStats.PremiumCurrency);
                p.Add("@Perception", updateCharacterStats.Perception);
                p.Add("@Acrobatics", updateCharacterStats.Acrobatics);
                p.Add("@Climb", updateCharacterStats.Climb);
                p.Add("@Stealth", updateCharacterStats.Stealth);
                p.Add("@Score", updateCharacterStats.Score);

                await Connection.QuerySingleOrDefaultAsync("UpdateCharacterStats",
                    p,
                    commandType: CommandType.StoredProcedure);
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
                p.Add("@Z", Z);
                p.Add("@RX", RX);
                p.Add("@RY", RY);
                p.Add("@RZ", RZ);

                await Connection.ExecuteAsync("UpdatePositionOfCharacterByName",
                    p,
                    commandType: CommandType.StoredProcedure);
            }
        }

        public async Task PlayerLogout(Guid customerGUID, string characterName)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);

                await Connection.ExecuteAsync("PlayerLogout",
                    p,
                    commandType: CommandType.StoredProcedure);
            }
        }

        public async Task AddAbilityToCharacter(Guid customerGUID, string abilityName, string characterName, int abilityLevel, string charHasAbilitiesCustomJSON)
        {
            using (Connection)
            {
                var paremeters = new
                {
                    CustomerGUID = customerGUID,
                    AbilityName = abilityName,
                    CharacterName = characterName,
                    AbilityLevel = abilityLevel,
                    CharHasAbilitiesCustomJSON = charHasAbilitiesCustomJSON
                };

                var getWorldServerID = await Connection.ExecuteAsync(MSSQLQueries.AddAbilityToCharacterSQL, paremeters);
            }
        }

        public async Task<IEnumerable<Abilities>> GetAbilities(Guid customerGUID)
        {
            IEnumerable<Abilities> outputGetAbilities;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);

                outputGetAbilities = await Connection.QueryAsync<Abilities>("GetAbilities",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return outputGetAbilities;
        }

        public async Task<IEnumerable<GetCharacterAbilities>> GetCharacterAbilities(Guid customerGUID, string characterName)
        {
            IEnumerable<GetCharacterAbilities> outputGetCharacterAbilities;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);

                outputGetCharacterAbilities = await Connection.QueryAsync<GetCharacterAbilities>("GetCharacterAbilities",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return outputGetCharacterAbilities;
        }

        public async Task<IEnumerable<GetAbilityBars>> GetAbilityBars(Guid customerGUID, string characterName)
        {
            IEnumerable<GetAbilityBars> outputGetAbilityBars;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);

                outputGetAbilityBars = await Connection.QueryAsync<GetAbilityBars>("GetAbilityBars",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return outputGetAbilityBars;
        }

        public async Task<IEnumerable<GetAbilityBarsAndAbilities>> GetAbilityBarsAndAbilities(Guid customerGUID, string characterName)
        {
            IEnumerable<GetAbilityBarsAndAbilities> outputGetAbilityBarsAndAbilities;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);

                outputGetAbilityBarsAndAbilities = await Connection.QueryAsync<GetAbilityBarsAndAbilities>("GetAbilityBarsAndAbilities",
                    p,
                    commandType: CommandType.StoredProcedure);
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

                await Connection.ExecuteAsync(MSSQLQueries.RemoveAbilityFromCharacterSQL, parameters);
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

                await Connection.ExecuteAsync(MSSQLQueries.UpdateAbilityOnCharacterSQL, parameters);
            }
        }
    }
}
