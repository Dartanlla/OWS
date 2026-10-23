using System;

namespace OWSData.Models.Composites
{
    // Read models for the console's Insight page and its trade and drop views. Accounts and characters of PIE runs are
    // left out, as in the level distribution.

    /// <summary>Accounts created on one day, and how far they got.</summary>
    public class NewPlayerCohort
    {
        public DateTime Day { get; set; }
        public int Accounts { get; set; }
        public int MadeCharacter { get; set; }
        public int Level5 { get; set; }
        public int Level10 { get; set; }
        /// <summary>Played on the day after the account was made.</summary>
        public int NextDay { get; set; }
        /// <summary>Played 7 or more days after the account was made.</summary>
        public int AfterAWeek { get; set; }
    }

    /// <summary>A character made in the last 30 days that has not played for 3 days, as it was when it stopped.</summary>
    public class StoppedPlayer
    {
        public string CharacterName { get; set; }
        public DateTime CreateDate { get; set; }
        public int Level { get; set; }
        public string MapName { get; set; }
        public DateTime? LastSeen { get; set; }
        public string LastQuest { get; set; }
        public string LastQuestEvent { get; set; }
        public double MinutesPlayed { get; set; }
    }

    public class QuestFunnelRow
    {
        public string Quest { get; set; }
        public int Accepted { get; set; }
        public int Completed { get; set; }
        public double? MedianMinutes { get; set; }
    }

    /// <summary>A character whose rate is over 3 times the median of the last 7 days, with at least 1 hour played.</summary>
    public class OutlierRow
    {
        public string CharacterName { get; set; }
        public double Hours { get; set; }
        public double LongestSessionHours { get; set; }
        public double JuriPerHour { get; set; }
        public double LevelsPerHour { get; set; }
        public double LootPerHour { get; set; }
        public double MedianJuriPerHour { get; set; }
        public double MedianLevelsPerHour { get; set; }
        public double MedianLootPerHour { get; set; }
        public double MedianLongestSessionHours { get; set; }
    }

    /// <summary>One character's trades with one partner.</summary>
    public class TradePartnerRow
    {
        public string Partner { get; set; }
        public int Trades { get; set; }
        public long JuriGiven { get; set; }
        public long JuriReceived { get; set; }
        public int ItemsGiven { get; set; }
        public int ItemsReceived { get; set; }
        public DateTime LastTrade { get; set; }
    }

    /// <summary>Trades between two characters over 7 days. A is the name that sorts first.</summary>
    public class TradePairRow
    {
        public string A { get; set; }
        public string B { get; set; }
        public int Trades { get; set; }
        public long AGaveJuri { get; set; }
        public long BGaveJuri { get; set; }
        public int AGaveItems { get; set; }
        public int BGaveItems { get; set; }
    }

    public class DropMobRow
    {
        public string Mob { get; set; }
        public long Kills { get; set; }
    }

    /// <summary>
    /// One loot table entry over 7 days: Chance is the configured chance; ExpectedDrops and Variance include the rate
    /// multiplier in force at each roll.
    /// </summary>
    public class DropItemRow
    {
        public string Mob { get; set; }
        public string Item { get; set; }
        public long Rolls { get; set; }
        public long Drops { get; set; }
        public double Chance { get; set; }
        public double ExpectedDrops { get; set; }
        public double Variance { get; set; }
        /// <summary>A deviation for drops that a 0% or 100% chance cannot produce.</summary>
        public const double ImpossibleDeviation = 99;

        /// <summary>Standard deviations between observed and expected drops; null under 50 rolls.</summary>
        public double? Deviation => Rolls < 50 ? null
            : Variance > 0 ? (Drops - ExpectedDrops) / Math.Sqrt(Variance)
            : Math.Abs(Drops - ExpectedDrops) < 0.5 ? 0 : Math.CopySign(ImpossibleDeviation, Drops - ExpectedDrops);
    }
}
