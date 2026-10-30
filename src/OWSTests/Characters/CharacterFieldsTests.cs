using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.SQL;
using Xunit;

namespace OWSTests.Characters
{
    public class CharacterFieldsTests
    {
        private static readonly string[] RemovedFields =
        {
            "Email",
            "IsAdmin",
            "IsModerator",
            "Perception",
            "Acrobatics",
            "Climb",
            "Stealth",
            "Spirit",
            "Magic",
            "TeamNumber",
            "Thirst",
            "Hunger",
            "Gold",
            "Score",
            "CharacterLevel",
            "Gender",
            "Xp",
            "HitDie",
            "Wounds",
            "Size",
            "Weight",
            "MaxHealth",
            "Health",
            "HealthRegenRate",
            "MaxMana",
            "Mana",
            "ManaRegenRate",
            "MaxEnergy",
            "Energy",
            "EnergyRegenRate",
            "MaxFatigue",
            "Fatigue",
            "FatigueRegenRate",
            "MaxStamina",
            "Stamina",
            "StaminaRegenRate",
            "MaxEndurance",
            "Endurance",
            "EnduranceRegenRate",
            "Strength",
            "Dexterity",
            "Constitution",
            "Intellect",
            "Wisdom",
            "Charisma",
            "Agility",
            "Fortitude",
            "Reflex",
            "Willpower",
            "BaseAttack",
            "BaseAttackBonus",
            "AttackPower",
            "AttackSpeed",
            "CritChance",
            "CritMultiplier",
            "Haste",
            "SpellPower",
            "SpellPenetration",
            "Defense",
            "Dodge",
            "Parry",
            "Avoidance",
            "Versatility",
            "Multishot",
            "Initiative",
            "NaturalArmor",
            "PhysicalArmor",
            "BonusArmor",
            "ForceArmor",
            "MagicArmor",
            "Resistance",
            "ReloadSpeed",
            "Range",
            "Speed",
            "Silver",
            "Copper",
            "FreeCurrency",
            "PremiumCurrency",
            "Fame",
            "Alignment",
            "Description",
        };

        [Theory]
        [InlineData(typeof(OWSData.Models.Tables.Characters))]
        [InlineData(typeof(GetCharByCharName))]
        [InlineData(typeof(GetAllCharacters))]
        [InlineData(typeof(CreateCharacter))]
        [InlineData(typeof(UpdateCharacterStats))]
        public void CharacterContractsOmitRemovedFields(Type type)
        {
            var names = type.GetProperties().Select(p => p.Name).ToArray();
            var json = JsonSerializer.Serialize(Activator.CreateInstance(type), type);
            using var document = JsonDocument.Parse(json);
            var jsonNames = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
            foreach (var field in RemovedFields)
            {
                Assert.DoesNotContain(names, n => string.Equals(n, field, StringComparison.OrdinalIgnoreCase));
                Assert.DoesNotContain(jsonNames, n => string.Equals(n, field, StringComparison.OrdinalIgnoreCase));
            }
            Assert.DoesNotContain("level", jsonNames);
        }

        [Fact]
        public void CreationAndUpdateSqlDoNotReferenceRemovedFields()
        {
            foreach (var sql in new[] { GenericQueries.CreateCharacterSQL, GenericQueries.UpdateCharacterStats,
                PostgresQueries.AddCharacterUsingDefaultCharacterValues,
                MSSQLQueries.AddCharacterUsingDefaultCharacterValues,
                MySQLQueries.AddCharacterUsingDefaultCharacterValues })
            {
                foreach (var field in RemovedFields)
                    Assert.False(Regex.IsMatch(sql, @"\b" + field + @"\b", RegexOptions.IgnoreCase), field);
            }
        }

        [Fact]
        public void LegacyStatsPayloadStillUpdatesPositionAndRotation()
        {
            var update = JsonSerializer.Deserialize<UpdateCharacterStats>(
                "{\"CharName\":\"Hero\",\"X\":1,\"Y\":2,\"Z\":3,\"RX\":4,\"RY\":5,\"RZ\":6,\"Health\":100,\"Gold\":5}");
            Assert.Equal("Hero", update.CharName);
            Assert.Equal(new float[] { 1, 2, 3, 4, 5, 6 }, new[] { update.X, update.Y, update.Z, update.RX, update.RY, update.RZ });
            var parameters = Regex.Matches(GenericQueries.UpdateCharacterStats, @"@(\w+)")
                .Select(m => m.Groups[1].Value).OrderBy(n => n).ToArray();
            Assert.Equal(typeof(UpdateCharacterStats).GetProperties().Select(p => p.Name).OrderBy(n => n), parameters);
        }
    }
}
