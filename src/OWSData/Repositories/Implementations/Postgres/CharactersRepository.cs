﻿using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSData.Models.Tables;
using OWSData.SQL;

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
            // TODO Add Logging

            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@CharName", characterName);
                parameters.Add("@MapInstanceID", mapInstanceID);

                var outputCharacter = await Connection.QuerySingleOrDefaultAsync<Characters>(GenericQueries.GetCharacterIDFromName,
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
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@CharName", addOrUpdateCustomCharacterData.CharacterName);
                p.Add("@CustomFieldName", addOrUpdateCustomCharacterData.CustomFieldName);
                p.Add("@FieldValue", addOrUpdateCustomCharacterData.FieldValue);

                await Connection.ExecuteAsync("call AddOrUpdateCustomCharData(@CustomerGUID,@CharName,@CustomFieldName,@FieldValue)",
                    p,
                    commandType: CommandType.Text);
            }
        }

        public async Task<MapInstances> CheckMapInstanceStatus(Guid customerGUID, int mapInstanceID)
        {
            MapInstances outputObject;
            using (Connection)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@MapInstanceID", mapInstanceID);

                outputObject = await Connection.QuerySingleOrDefaultAsync<MapInstances>(GenericQueries.GetMapInstanceStatus,
                    parameters,
                    commandType: CommandType.Text);
            }

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

                outputCharacter = await Connection.QuerySingleOrDefaultAsync<GetCharByCharName>("select * from GetCharByCharName(@CustomerGUID,@CharName)",
                    p,
                    commandType: CommandType.Text);
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

                outputCustomCharacterDataRows = await Connection.QueryAsync<CustomCharacterData>("select * from GetCustomCharData(@CustomerGUID,@CharName)",
                    p,
                    commandType: CommandType.Text);
            }

            return outputCustomCharacterDataRows;
        }

        public async Task<JoinMapByCharName> JoinMapByCharName(Guid customerGUID, string characterName, string zoneName, int playerGroupType)
        {
            JoinMapByCharName outputObject;

            try
            {

                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@CharName", characterName);
                    p.Add("@ZoneName", zoneName);
                    p.Add("@PlayerGroupType", playerGroupType);

                    outputObject = await Connection.QuerySingleOrDefaultAsync<JoinMapByCharName>("select * from JoinMapByCharName(@CustomerGUID,@CharName,@ZoneName,@PlayerGroupType)",
                        p,
                        commandType: CommandType.Text);
                }
                return outputObject;
            }
            catch (Exception) {
                outputObject = new JoinMapByCharName
                {
                    WorldServerID = -1,
                    MapInstanceStatus = -1
                };
                return outputObject;
            }
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
                                                           "UpdateCharacterStats(@CustomerGUID::uuid, " +
                                                           "@CharName::varchar, " +
                                                           "@CharacterLevel::smallint, " +
                                                           "@Gender::smallint, " +
                                                           "@Weight::float, " +
                                                           "@Size::smallint, " +
                                                           "@Fame::float, " +
                                                           "@Alignment::float, " +
                                                           "@Description::text, " +
                                                           "@XP, " +
                                                           "@X::float, " +
                                                           "@Y::float, " +
                                                           "@Z::float, " +
                                                           "@RX::float, " +
                                                           "@RY::float, " +
                                                           "@RZ::float, " +
                                                           "@TeamNumber, " +
                                                           "@HitDie::smallint, " +
                                                           "@Wounds::float, " +
                                                           "@Thirst::float, " +
                                                           "@Hunger::float, " +
                                                           "@MaxHealth::float, " +
                                                           "@Health::float, " +
                                                           "@HealthRegenRate::float, " +
                                                           "@MaxMana::float, " +
                                                           "@Mana::float, " +
                                                           "@ManaRegenRate::float, " +
                                                           "@MaxEnergy::float, " +
                                                           "@Energy::float, " +
                                                           "@EnergyRegenRate::float, " +
                                                           "@MaxFatigue::float, " +
                                                           "@Fatigue::float, " +
                                                           "@FatigueRegenRate::float, " +
                                                           "@MaxStamina::float, " +
                                                           "@Stamina::float, " +
                                                           "@StaminaRegenRate::float, " +
                                                           "@MaxEndurance::float, " +
                                                           "@Endurance::float, " +
                                                           "@EnduranceRegenRate::float, " +
                                                           "@Strength::float, " +
                                                           "@Dexterity::float, " +
                                                           "@Constitution::float, " +
                                                           "@Intellect::float, " +
                                                           "@Wisdom::float, " +
                                                           "@Charisma::float, " +
                                                           "@Agility::float, " +
                                                           "@Spirit::float, " +
                                                           "@Magic::float, " +
                                                           "@Fortitude::float, " +
                                                           "@Reflex::float, " +
                                                           "@Willpower::float, " +
                                                           "@BaseAttack::float, " +
                                                           "@BaseAttackBonus::float, " +
                                                           "@AttackPower::float, " +
                                                           "@AttackSpeed::float, " +
                                                           "@CritChance::float, " +
                                                           "@CritMultiplier::float, " +
                                                           "@Haste::float, " +
                                                           "@SpellPower::float, " +
                                                           "@SpellPenetration::float, " +
                                                           "@Defense::float, " +
                                                           "@Dodge::float, " +
                                                           "@Parry::float, " +
                                                           "@Avoidance::float, " +
                                                           "@Versatility::float, " +
                                                           "@Multishot::float, " +
                                                           "@Initiative::float, " +
                                                           "@NaturalArmor::float, " +
                                                           "@PhysicalArmor::float, " +
                                                           "@BonusArmor::float, " +
                                                           "@ForceArmor::float, " +
                                                           "@MagicArmor::float, " +
                                                           "@Resistance::float, " +
                                                           "@ReloadSpeed::float, " +
                                                           "@Range::float, " +
                                                           "@Speed::float, " +
                                                           "@Gold, " +
                                                           "@Silver, " +
                                                           "@Copper, " +
                                                           "@FreeCurrency, " +
                                                           "@PremiumCurrency, " +
                                                           "@Perception::float, " +
                                                           "@Acrobatics::float, " +
                                                           "@Climb::float, " +
                                                           "@Stealth::float, " +
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

                await Connection.QueryAsync(PostgresQueries.AddAbilityToCharacterSQL,
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

                outputGetAbilities = await Connection.QueryAsync<Abilities>(PostgresQueries.GetAbilities,
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

                outputGetCharacterAbilities = await Connection.QueryAsync<GetCharacterAbilities>("select * from GetCharacterAbilities(@CustomerGUID, @CharName)",
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

                outputGetAbilityBars = await Connection.QueryAsync<GetAbilityBars>("select * from GetAbilityBars(@CustomerGUID, @CharName)",
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

                outputGetAbilityBarsAndAbilities = await Connection.QueryAsync<GetAbilityBarsAndAbilities>("select * from GetAbilityBarsAndAbilities (@CustomerGUID, @CharName)",
                    p,
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

                await Connection.ExecuteAsync(PostgresQueries.RemoveAbilityFromCharacterSQL, parameters);
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

                await Connection.ExecuteAsync(PostgresQueries.UpdateAbilityOnCharacterSQL, parameters);
            }
        }
    }
}
