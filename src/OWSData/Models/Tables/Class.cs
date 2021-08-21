using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class Class
    {
        public Guid CustomerGuid { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string StartingMapName { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Perception { get; set; }
        public double Acrobatics { get; set; }
        public double Climb { get; set; }
        public double Stealth { get; set; }
        public double Rx { get; set; }
        public double Ry { get; set; }
        public double Rz { get; set; }
        public double Spirit { get; set; }
        public double Magic { get; set; }
        public int TeamNumber { get; set; }
        public double Thirst { get; set; }
        public double Hunger { get; set; }
        public int Gold { get; set; }
        public int Score { get; set; }
        public short CharacterLevel { get; set; }
        public byte Gender { get; set; }
        public int Xp { get; set; }
        public byte HitDie { get; set; }
        public double Wounds { get; set; }
        public short Size { get; set; }
        public double Weight { get; set; }
        public double MaxHealth { get; set; }
        public double Health { get; set; }
        public double HealthRegenRate { get; set; }
        public double MaxMana { get; set; }
        public double Mana { get; set; }
        public double ManaRegenRate { get; set; }
        public double MaxEnergy { get; set; }
        public double Energy { get; set; }
        public double EnergyRegenRate { get; set; }
        public double MaxFatigue { get; set; }
        public double Fatigue { get; set; }
        public double FatigueRegenRate { get; set; }
        public double MaxStamina { get; set; }
        public double Stamina { get; set; }
        public double StaminaRegenRate { get; set; }
        public double MaxEndurance { get; set; }
        public double Endurance { get; set; }
        public double EnduranceRegenRate { get; set; }
        public double Strength { get; set; }
        public double Dexterity { get; set; }
        public double Constitution { get; set; }
        public double Intellect { get; set; }
        public double Wisdom { get; set; }
        public double Charisma { get; set; }
        public double Agility { get; set; }
        public double Fortitude { get; set; }
        public double Reflex { get; set; }
        public double Willpower { get; set; }
        public double BaseAttack { get; set; }
        public double BaseAttackBonus { get; set; }
        public double AttackPower { get; set; }
        public double AttackSpeed { get; set; }
        public double CritChance { get; set; }
        public double CritMultiplier { get; set; }
        public double Haste { get; set; }
        public double SpellPower { get; set; }
        public double SpellPenetration { get; set; }
        public double Defense { get; set; }
        public double Dodge { get; set; }
        public double Parry { get; set; }
        public double Avoidance { get; set; }
        public double Versatility { get; set; }
        public double Multishot { get; set; }
        public double Initiative { get; set; }
        public double NaturalArmor { get; set; }
        public double PhysicalArmor { get; set; }
        public double BonusArmor { get; set; }
        public double ForceArmor { get; set; }
        public double MagicArmor { get; set; }
        public double Resistance { get; set; }
        public double ReloadSpeed { get; set; }
        public double Range { get; set; }
        public double Speed { get; set; }
        public int Silver { get; set; }
        public int Copper { get; set; }
        public int FreeCurrency { get; set; }
        public int PremiumCurrency { get; set; }
        public double Fame { get; set; }
        public double Alignment { get; set; }
        public string Description { get; set; }
    }
}
