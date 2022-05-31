using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSData.Models;
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
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);
                p.Add("@MapInstanceID", mapInstanceID);

                await Connection.QuerySingleOrDefaultAsync("AddCharacterToMapInstanceByCharName",
                    p,
                    commandType: CommandType.StoredProcedure);
            }
        }

        public async Task AddOrUpdateCustomCharacterData(Guid customerGUID, AddOrUpdateCustomCharacterData addOrUpdateCustomCharacterData)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", addOrUpdateCustomCharacterData.CharacterName);
                p.Add("@CustomFieldName", addOrUpdateCustomCharacterData.CustomFieldName);
                p.Add("@FieldValue", addOrUpdateCustomCharacterData.FieldValue);

                await Connection.ExecuteAsync("AddOrUpdateCustomCharData",
                    p,
                    commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<CheckMapInstanceStatus> CheckMapInstanceStatus(Guid customerGUID, int mapInstanceID)
        {
            CheckMapInstanceStatus outputObject = new CheckMapInstanceStatus();

            int mapInstanceStatus = 0;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@MapInstanceID", mapInstanceID);
                p.Add("@MapInstanceStatus", mapInstanceStatus, DbType.Int32, ParameterDirection.Output);

                await Connection.QuerySingleOrDefaultAsync("CheckMapInstanceStatus",
                    p,
                    commandType: CommandType.StoredProcedure);

                mapInstanceStatus = p.Get<int>("@MapInstanceStatus");
            }

            outputObject = new CheckMapInstanceStatus(mapInstanceStatus);
            return outputObject;
        }

        public async Task<GetCharByCharName> GetCharByCharName(Guid customerGUID, string characterName)
        {
            GetCharByCharName outputCharacter;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);
                p.Add("@EnableAutoLoopBack", dbType: DbType.Boolean, direction: ParameterDirection.Output);

                outputCharacter = await Connection.QuerySingleOrDefaultAsync<GetCharByCharName>("GetCharByCharName",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return outputCharacter;
        }

        public async Task<IEnumerable<CustomCharacterData>> GetCustomCharacterData(Guid customerGUID, string characterName)
        {
            IEnumerable<CustomCharacterData> outputCustomCharacterDataRows;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);

                outputCustomCharacterDataRows = await Connection.QueryAsync<CustomCharacterData>("GetCustomCharData",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return outputCustomCharacterDataRows;
        }

        public async Task<JoinMapByCharName> JoinMapByCharName(Guid customerGUID, string characterName, string zoneName, int playerGroupType)
        {
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
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", characterName);
                p.Add("@ZoneName", zoneName);
                p.Add("@PlayerGroupType", playerGroupType);

                p.Add("@ServerIP", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);
                p.Add("@WorldServerID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@WorldServerIP", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);
                p.Add("@WorldServerPort", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@Port", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@MapInstanceID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@MapNameToStart", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);
                p.Add("@MapInstanceStatus", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@NeedToStartupMap", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("@EnableAutoLoopback", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("@NoPortForwarding", noPortForwarding, dbType: DbType.Boolean, direction: ParameterDirection.Output);

                await Connection.QuerySingleOrDefaultAsync("JoinMapByCharName",
                    p,
                    commandType: CommandType.StoredProcedure);

                serverIp = p.Get<string>("@ServerIP");
                worldServerId = p.Get<int?>("@WorldServerID");
                worldServerIp = p.Get<string>("@WorldServerIP");
                worldServerPort = p.Get<int>("@WorldServerPort");
                port = p.Get<int>("@Port");
                mapInstanceID = p.Get<int>("@MapInstanceID");
                mapNameToStart = p.Get<string>("@MapNameToStart");
                mapInstanceStatus = p.Get<int?>("@MapInstanceStatus");
                needToStartupMap = p.Get<bool>("@NeedToStartupMap");
                enableAutoLoopback = p.Get<bool>("@EnableAutoLoopback");
                noPortForwarding = p.Get<bool>("@NoPortForwarding");
            }

            outputObject = new JoinMapByCharName() {
                ServerIP = serverIp,
                Port = port,
                WorldServerID = worldServerId ?? -1,
                WorldServerIP = worldServerIp,
                WorldServerPort = worldServerPort,
                MapInstanceID = mapInstanceID,
                MapNameToStart = mapNameToStart,
                MapInstanceStatus = mapInstanceStatus ?? -1,
                NeedToStartupMap = needToStartupMap,
                EnableAutoLoopback = enableAutoLoopback,
                NoPortForwarding = noPortForwarding
            };

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
                var paremeters = new
                {
                    CustomerGUID = customerGUID,
                    AbilityName = abilityName,
                    CharacterName = characterName
                };

                var getWorldServerID = await Connection.ExecuteAsync(MSSQLQueries.RemoveAbilityFromCharacterSQL, paremeters);
            }
        }

        public async Task UpdateAbilityOnCharacter(Guid customerGUID, string abilityName, string characterName, int abilityLevel, string charHasAbilitiesCustomJSON)
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

                var getWorldServerID = await Connection.ExecuteAsync(MSSQLQueries.UpdateAbilityOnCharacterSQL, paremeters);
            }
        }
    }
}
