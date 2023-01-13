using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using Dapper.Transaction;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSData.Models.Tables;
using OWSData.SQL;

namespace OWSData.Repositories.Implementations.MySQL
{
    public class CharactersRepository : ICharactersRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public CharactersRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private IDbConnection Connection => new MySqlConnection(_storageOptions.Value.OWSDBConnectionString);

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
                    parameters.Add("@CharacterMinutes", 1); // TODO Add Configuration Parameter
                    parameters.Add("@MapMinutes", 2); // TODO Add Configuration Parameter

                    await transaction.ExecuteAsync(MySQLQueries.RemoveCharactersFromAllInactiveInstances,
                        parameters,
                        commandType: CommandType.Text);

                    var outputMapInstances = await transaction.QueryAsync<int>(MySQLQueries.GetAllInactiveMapInstances,
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

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);
                parameters.Add("@ZoneName", zoneName);
                parameters.Add("@PlayerGroupType", playerGroupType);

                Maps outputMap = await Connection.QuerySingleOrDefaultAsync<Maps>(GenericQueries.GetMapByZoneName,
                    parameters,
                    commandType: CommandType.Text);

                Characters outputCharacter = await Connection.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterByName,
                    parameters,
                    commandType: CommandType.Text);

                Customers outputCustomer = await Connection.QuerySingleOrDefaultAsync<Customers>(GenericQueries.GetCustomer,
                    parameters,
                    commandType: CommandType.Text);

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
                        NeedToStartupMap = false,
                        EnableAutoLoopback = enableAutoLoopback,
                        NoPortForwarding = noPortForwarding
                    };

                    return outputObject;
                }

                PlayerGroup outputPlayerGroup = new PlayerGroup();

                if (playerGroupType > 0)
                {
                    outputPlayerGroup = await Connection.QuerySingleOrDefaultAsync<PlayerGroup>(GenericQueries.GetPlayerGroupIDByType,
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

                JoinMapByCharName outputJoinMapByCharName = await Connection.QuerySingleOrDefaultAsync<JoinMapByCharName>(GenericQueries.GetMapInstancesByMapAndGroup,
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

                    WorldServers outputWorldServers =  await Connection.QuerySingleOrDefaultAsync<WorldServers>(GenericQueries.GetWorldByID,
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

                await Connection.QuerySingleOrDefaultAsync("call " +
                                                           "UpdateCharacterStats(@CustomerGUID," +
                                                           "@CharName," +
                                                           "@CharacterLevel," +
                                                           "@Gender," +
                                                           "@Weight," +
                                                           "@Size," +
                                                           "@Fame," +
                                                           "@Alignment," +
                                                           "@Description," +
                                                           "@XP," +
                                                           "@X," +
                                                           "@Y," +
                                                           "@Z," +
                                                           "@RX," +
                                                           "@RY," +
                                                           "@RZ," +
                                                           "@TeamNumber," +
                                                           "@HitDie," +
                                                           "@Wounds," +
                                                           "@Thirst," +
                                                           "@Hunger," +
                                                           "@MaxHealth," +
                                                           "@Health," +
                                                           "@HealthRegenRate," +
                                                           "@MaxMana," +
                                                           "@Mana," +
                                                           "@ManaRegenRate," +
                                                           "@MaxEnergy," +
                                                           "@Energy," +
                                                           "@EnergyRegenRate," +
                                                           "@MaxFatigue," +
                                                           "@Fatigue," +
                                                           "@FatigueRegenRate," +
                                                           "@MaxStamina," +
                                                           "@Stamina," +
                                                           "@StaminaRegenRate," +
                                                           "@MaxEndurance," +
                                                           "@Endurance," +
                                                           "@EnduranceRegenRate," +
                                                           "@Strength," +
                                                           "@Dexterity," +
                                                           "@Constitution," +
                                                           "@Intellect," +
                                                           "@Wisdom," +
                                                           "@Charisma," +
                                                           "@Agility," +
                                                           "@Spirit," +
                                                           "@Magic," +
                                                           "@Fortitude," +
                                                           "@Reflex," +
                                                           "@Willpower," +
                                                           "@BaseAttack," +
                                                           "@BaseAttackBonus," +
                                                           "@AttackPower," +
                                                           "@AttackSpeed," +
                                                           "@CritChance," +
                                                           "@CritMultiplier," +
                                                           "@Haste," +
                                                           "@SpellPower," +
                                                           "@SpellPenetration," +
                                                           "@Defense," +
                                                           "@Dodge," +
                                                           "@Parry," +
                                                           "@Avoidance," +
                                                           "@Versatility," +
                                                           "@Multishot," +
                                                           "@Initiative," +
                                                           "@NaturalArmor," +
                                                           "@PhysicalArmor," +
                                                           "@BonusArmor," +
                                                           "@ForceArmor," +
                                                           "@MagicArmor," +
                                                           "@Resistance," +
                                                           "@ReloadSpeed," +
                                                           "@Range," +
                                                           "@Speed," +
                                                           "@Gold," +
                                                           "@Silver," +
                                                           "@Copper," +
                                                           "@FreeCurrency," +
                                                           "@PremiumCurrency," +
                                                           "@Perception," +
                                                           "@Acrobatics," +
                                                           "@Climb," +
                                                           "@Stealth," +
                                                           "@Score)",
                    p,
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
                p.Add("@Z", Z);
                p.Add("@RX", RX);
                p.Add("@RY", RY);
                p.Add("@RZ", RZ);

                await Connection.ExecuteAsync("call UpdatePositionOfCharacterByName (@CustomerGUID,@CharName,@MapName,@X,@Y,@Z,@RX,@RY,@RZ)",
                    p,
                    commandType: CommandType.Text);
            }
        }

        public async Task PlayerLogout(Guid customerGUID, string characterName)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);

                await Connection.ExecuteAsync("call PlayerLogout(@CustomerGUID, @CharName)",
                    p,
                    commandType: CommandType.Text);
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

                await Connection.ExecuteAsync(MySQLQueries.AddAbilityToCharacterSQL,
                    parameters,
                    commandType: CommandType.Text);
            }
        }

        public async Task<IEnumerable<Abilities>> GetAbilities(Guid customerGUID)
        {
            IEnumerable<Abilities> outputGetAbilities;

            using (Connection)
            {
                var parameters = new
                {
                    CustomerGUID = customerGUID
                };

                outputGetAbilities = await Connection.QueryAsync<Abilities>(MySQLQueries.GetAbilities,
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
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);

                outputGetCharacterAbilities = await Connection.QueryAsync<GetCharacterAbilities>("call GetCharacterAbilities(@CustomerGUID, @CharName)",
                    p,
                    commandType: CommandType.Text);
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

                outputGetAbilityBars = await Connection.QueryAsync<GetAbilityBars>("call GetAbilityBars(@CustomerGUID, @CharName)",
                    p,
                    commandType: CommandType.Text);
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

                outputGetAbilityBarsAndAbilities = await Connection.QueryAsync<GetAbilityBarsAndAbilities>("call GetAbilityBarsAndAbilities (@CustomerGUID, @CharName)",
                    p,
                    commandType: CommandType.Text);
            }

            return outputGetAbilityBarsAndAbilities;
        }

         public async Task<MapInstances> SpinUpInstance(Guid customerGUID, string zoneName, int playerGroupId = 0)
        {
            // TODO Add Logging

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@ZoneName", zoneName);
                parameters.Add("@PlayerGroupId", playerGroupId);

                List<WorldServers> outputWorldServers = (List<WorldServers>)await Connection.QueryAsync<WorldServers>(GenericQueries.GetActiveWorldServersByLoad,
                    parameters,
                    commandType: CommandType.Text);

                if (outputWorldServers.Any())
                {
                    int? firstAvailable = null;
                    foreach (var worldServer in outputWorldServers)
                    {
                        var portsInUse = await Connection.QueryAsync<int>(GenericQueries.GetPortsInUseByWorldServer,
                            parameters,
                            commandType: CommandType.Text);

                        firstAvailable = Enumerable.Range(worldServer.StartingMapInstancePort, worldServer.StartingMapInstancePort + worldServer.MaxNumberOfInstances)
                            .Except(portsInUse)
                            .FirstOrDefault();

                        if (firstAvailable >= worldServer.StartingMapInstancePort)
                        {

                            Maps outputMaps = await Connection.QuerySingleOrDefaultAsync<Maps>(GenericQueries.GetMapByZoneName,
                                parameters,
                                commandType: CommandType.Text);

                            parameters.Add("@WorldServerID", worldServer.WorldServerId);
                            parameters.Add("@MapID", outputMaps.MapId);
                            parameters.Add("@Port", firstAvailable);

                            int outputMapInstanceID = await Connection.QuerySingleOrDefaultAsync<int>(MySQLQueries.AddMapInstance,
                                parameters,
                                commandType: CommandType.Text);

                            parameters.Add("@MapInstanceID", outputMapInstanceID);

                            MapInstances outputMapInstances = await Connection.QuerySingleOrDefaultAsync<MapInstances>(GenericQueries.GetMapInstance,
                                parameters,
                                commandType: CommandType.Text);

                            return outputMapInstances;
                        }
                    }
                }
            }

            return new MapInstances { MapInstanceId = -1 };
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

                await Connection.ExecuteAsync(MySQLQueries.RemoveAbilityFromCharacterSQL, parameters);
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

                await Connection.ExecuteAsync(MySQLQueries.UpdateAbilityOnCharacterSQL, parameters);
            }
        }
    }
}
