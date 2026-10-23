using System;
using System.Collections.Generic;
using OWSData.Models.Composites;
using Xunit;

namespace OWSTests.Models
{
    /// <summary>The live-operations settings zones read through ZoneSync, and the drop-rate check.</summary>
    public class SamsaraOpsTests
    {
        private static Dictionary<string, string> Settings(string key, string value) => new() { [key] = value };

        [Fact]
        public void Rates_ClampMultipliersAndReadUtc()
        {
            RateEvent rates = ConsoleSettingValues.Rates(Settings(ConsoleSettingKeys.RateEvent,
                "{\"Name\":\"Harvest\",\"Exp\":9,\"Drop\":0.5,\"StartsAt\":\"2026-10-22T10:00:00Z\",\"EndsAt\":\"2026-10-22T12:00:00Z\"}"));

            Assert.Equal(5, rates.Exp);
            Assert.Equal(1, rates.Drop);
            Assert.True(rates.InForce(new DateTime(2026, 10, 22, 11, 0, 0, DateTimeKind.Utc)));
            Assert.False(rates.InForce(new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Theory]
        [InlineData("{\"Name\":\"Backwards\",\"Exp\":2,\"StartsAt\":\"2026-10-22T12:00:00Z\",\"EndsAt\":\"2026-10-22T10:00:00Z\"}")]
        [InlineData("not json")]
        [InlineData("")]
        public void Rates_IgnoreUnusableValues(string value)
        {
            Assert.Null(ConsoleSettingValues.Rates(Settings(ConsoleSettingKeys.RateEvent, value)));
        }

        [Fact]
        public void Maintenance_StartsAtItsTimeWithADefaultMessage()
        {
            MaintenanceWindow window = ConsoleSettingValues.Maintenance(Settings(ConsoleSettingKeys.Maintenance,
                "{\"StartsAt\":\"2026-10-22T10:00:00Z\",\"Message\":\"  \"}"));

            Assert.False(window.Started(new DateTime(2026, 10, 22, 9, 59, 59, DateTimeKind.Utc)));
            Assert.True(window.Started(new DateTime(2026, 10, 22, 10, 0, 0, DateTimeKind.Utc)));
            Assert.Equal("The servers are closed for maintenance. Try again later.", window.PlayerMessage());
        }

        [Theory]
        [InlineData("40", 40)]
        [InlineData("0", 0)]
        [InlineData("-3", 0)]
        [InlineData("many", 0)]
        public void PlayerCap_IsOffUnlessPositive(string value, int cap)
        {
            Assert.Equal(cap, ConsoleSettingValues.PlayerCap(Settings(ConsoleSettingKeys.PlayerCap, value)));
        }

        [Fact]
        public void DropDeviation_NeedsFiftyRolls()
        {
            // 10% chance over 100 rolls: 10 expected, variance 9; 19 drops is 3 standard deviations high.
            var row = new DropItemRow { Rolls = 100, Drops = 19, ExpectedDrops = 10, Variance = 9 };
            Assert.Equal(3, row.Deviation);

            row.Rolls = 49;
            Assert.Null(row.Deviation);
        }

        [Fact]
        public void DropDeviation_FlagsAnyMissAtACertainChance()
        {
            // A 100% chance (or one capped there by a rate event) over 60 rolls: one miss is impossible, none is exact.
            var row = new DropItemRow { Rolls = 60, Drops = 59, ExpectedDrops = 60, Variance = 0 };
            Assert.Equal(-DropItemRow.ImpossibleDeviation, row.Deviation);

            row.Drops = 60;
            Assert.Equal(0, row.Deviation);
        }
    }
}
