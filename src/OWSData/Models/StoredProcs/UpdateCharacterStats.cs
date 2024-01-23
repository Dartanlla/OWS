using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    [Serializable]
    public record UpdateCharacterStats
    (
         string CharName,
         int CharacterLevel,
         int Gender,
         float Weight,
         int Size,
         float Fame,
         float Alignment,
         string Description,
         int XP,
         float X,
         float Y,
         float Z,
         float RX,
         float RY,
         float RZ,
         int TeamNumber,
         int HitDie,
         float Wounds,
         float Thirst,
         float Hunger,
         float MaxHealth,
         float Health,
         float HealthRegenRate,
         float MaxMana,
         float Mana,
         float ManaRegenRate,
         float MaxEnergy,
         float Energy,
         float EnergyRegenRate,
         float MaxFatigue,
         float Fatigue,
         float FatigueRegenRate,
         float MaxStamina,
         float Stamina,
         float StaminaRegenRate,
         float MaxEndurance,
         float Endurance,
         float EnduranceRegenRate,
         float Strength,
         float Dexterity,
         float Constitution,
         float Intellect,
         float Wisdom,
         float Charisma,
         float Agility,
         float Spirit,
         float Magic,
         float Fortitude,
         float Reflex,
         float Willpower,
         float BaseAttack,
         float BaseAttackBonus,
         float AttackPower,
         float AttackSpeed,
         float CritChance,
         float CritMultiplier,
         float Haste,
         float SpellPower,
         float SpellPenetration,
         float Defense,
         float Dodge,
         float Parry,
         float Avoidance,
         float Versatility,
         float Multishot,
         float Initiative,
         float NaturalArmor,
         float PhysicalArmor,
         float BonusArmor,
         float ForceArmor,
         float MagicArmor,
         float Resistance,
         float ReloadSpeed,
         float Range,
         float Speed,
         int Gold,
         int Silver,
         int Copper,
         int FreeCurrency,
         int PremiumCurrency,
         float Perception,
         float Acrobatics,
         float Climb,
         float Stealth,
         int Score,
         string CustomerGUID
    );

    //public class UpdateCharacterStats
    //{
    //    public string CharName { get; set; }
    //    public int CharacterLevel { get; set; }
    //    public int Gender { get; set; }
    //    public float Weight { get; set; }
    //    public int Size { get; set; }
    //    public float Fame { get; set; }
    //    public float Alignment { get; set; }
    //    public string Description { get; set; }
    //    public int XP { get; set; }
    //    public float X { get; set; }
    //    public float Y { get; set; }
    //    public float Z { get; set; }
    //    public float RX { get; set; }
    //    public float RY { get; set; }
    //    public float RZ { get; set; }
    //    public int TeamNumber { get; set; }
    //    public int HitDie { get; set; }
    //    public float Wounds { get; set; }
    //    public float Thirst { get; set; }
    //    public float Hunger { get; set; }
    //    public float MaxHealth { get; set; }
    //    public float Health { get; set; }
    //    public float HealthRegenRate { get; set; }
    //    public float MaxMana { get; set; }
    //    public float Mana { get; set; }
    //    public float ManaRegenRate { get; set; }
    //    public float MaxEnergy { get; set; }
    //    public float Energy { get; set; }
    //    public float EnergyRegenRate { get; set; }
    //    public float MaxFatigue { get; set; }
    //    public float Fatigue { get; set; }
    //    public float FatigueRegenRate { get; set; }
    //    public float MaxStamina { get; set; }
    //    public float Stamina { get; set; }
    //    public float StaminaRegenRate { get; set; }
    //    public float MaxEndurance { get; set; }
    //    public float Endurance { get; set; }
    //    public float EnduranceRegenRate { get; set; }
    //    public float Strength { get; set; }
    //    public float Dexterity { get; set; }
    //    public float Constitution { get; set; }
    //    public float Intellect { get; set; }
    //    public float Wisdom { get; set; }
    //    public float Charisma { get; set; }
    //    public float Agility { get; set; }
    //    public float Spirit { get; set; }
    //    public float Magic { get; set; }
    //    public float Fortitude { get; set; }
    //    public float Reflex { get; set; }
    //    public float Willpower { get; set; }
    //    public float BaseAttack { get; set; }
    //    public float BaseAttackBonus { get; set; }
    //    public float AttackPower { get; set; }
    //    public float AttackSpeed { get; set; }
    //    public float CritChance { get; set; }
    //    public float CritMultiplier { get; set; }
    //    public float Haste { get; set; }
    //    public float SpellPower { get; set; }
    //    public float SpellPenetration { get; set; }
    //    public float Defense { get; set; }
    //    public float Dodge { get; set; }
    //    public float Parry { get; set; }
    //    public float Avoidance { get; set; }
    //    public float Versatility { get; set; }
    //    public float Multishot { get; set; }
    //    public float Initiative { get; set; }
    //    public float NaturalArmor { get; set; }
    //    public float PhysicalArmor { get; set; }
    //    public float BonusArmor { get; set; }
    //    public float ForceArmor { get; set; }
    //    public float MagicArmor { get; set; }
    //    public float Resistance { get; set; }
    //    public float ReloadSpeed { get; set; }
    //    public float Range { get; set; }
    //    public float Speed { get; set; }
    //    public int Gold { get; set; }
    //    public int Silver { get; set; }
    //    public int Copper { get; set; }
    //    public int FreeCurrency { get; set; }
    //    public int PremiumCurrency { get; set; }
    //    public float Perception { get; set; }
    //    public float Acrobatics { get; set; }
    //    public float Climb { get; set; }
    //    public float Stealth { get; set; }
    //    public int Score { get; set; }
    //    public string CustomerGUID { get; set; }
    //}
}
