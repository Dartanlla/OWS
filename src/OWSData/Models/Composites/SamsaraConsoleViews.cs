using System;
using System.Collections.Generic;

namespace OWSData.Models.Composites
{
    // Read models for the staff console (OWSManagement). Level and job level come from CharStats; class comes from the
    // persistent character blob (classTag), because Characters.CharacterLevel and ClassID are creation-time values.

    public class ConsoleOverview
    {
        public int OnlineNow { get; set; }
        public int ZonesUp { get; set; }
        public int AccountsToday { get; set; }
        public int NewCharactersToday { get; set; }
        public double? MedianSessionMinutes7d { get; set; }
        public List<ConsoleSignal> Signals { get; set; } = new();
    }

    /// <summary>Something from the last 24 h a staff member should look at. Kind: save_failed, rejected, juri, zone_exit, duplicate.</summary>
    public class ConsoleSignal
    {
        public string Kind { get; set; }
        public string Subject { get; set; }
        public string Detail { get; set; }
        public long Count { get; set; }
        public DateTime? LastSeen { get; set; }
    }

    public class ConsoleAccountRow
    {
        public Guid UserGUID { get; set; }
        public string FirstName { get; set; }
        public string Email { get; set; }
        public string SteamId { get; set; }
        public string Role { get; set; }
        public string PreferredServerRegion { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastAccess { get; set; }
        public int CharacterCount { get; set; }
        public bool Banned { get; set; }
        public bool Muted { get; set; }
    }

    public class ConsoleCharacterRow
    {
        public int CharacterID { get; set; }
        public string CharName { get; set; }
        public Guid? UserGUID { get; set; }
        public string AccountName { get; set; }
        public string ClassTag { get; set; }
        public int? BaseLevel { get; set; }
        public int? JobLevel { get; set; }
        public string MapName { get; set; }
        public long Gold { get; set; }
        public DateTime LastActivity { get; set; }
        public DateTime CreateDate { get; set; }
        public int? OnlineMapInstanceID { get; set; }
        public string OnlineZone { get; set; }
    }

    public class ConsoleCharacterDetail : ConsoleCharacterRow
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public int Gender { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsModerator { get; set; }
        public bool IsInternalNetworkTestUser { get; set; }
        public long EconomyRevision { get; set; }
        public string Role { get; set; }
        public Dictionary<string, int> Stats { get; set; } = new();
        public List<ConsoleItem> Items { get; set; } = new();
        public List<ConsoleQuest> Quests { get; set; } = new();
        public List<ConsoleShop> Shops { get; set; } = new();
    }

    /// <summary>An inventory row with its CustomData decoded. Slots from 1000 up are equipped (1000 + equipment index).</summary>
    public class ConsoleItem
    {
        public const int EquipmentSlotBase = 1000;

        public int InSlotNumber { get; set; }
        public string ItemIDTag { get; set; }
        public int Quantity { get; set; }
        public Guid? InstanceId { get; set; }
        public string CustomData { get; set; }
        public bool Equipped => InSlotNumber >= EquipmentSlotBase;
        public int Slot => Equipped ? InSlotNumber - EquipmentSlotBase : InSlotNumber;
        public int Refine { get; set; }
        public int Stars { get; set; }
        public bool Locked { get; set; }
        public bool Damaged { get; set; }
        public List<string> Cards { get; set; } = new();
    }

    public class ConsoleQuest
    {
        public string QuestIDTag { get; set; }
        public string QuestJournalTagContainer { get; set; }
    }

    public class ConsoleShop
    {
        public long PlayerShopID { get; set; }
        public int OwnerCharacterID { get; set; }
        public string OwnerCharName { get; set; }
        public string ShopName { get; set; }
        public string ZoneName { get; set; }
        public string SaleStatus { get; set; }
        public string SettlementStatus { get; set; }
        public int ProceedsGoldPending { get; set; }
        public int Listings { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    public class ConsoleCount
    {
        public string Key { get; set; }
        public long Count { get; set; }
    }

    public class ConsoleDistribution
    {
        public List<ConsoleCount> Levels { get; set; } = new();
        public List<ConsoleCount> Classes { get; set; } = new();
    }

    public class ConsoleEconomy
    {
        public long JuriTotal { get; set; }
        public long JuriMedian { get; set; }
        public int Characters { get; set; }
        public List<ConsoleCharacterRow> TopHolders { get; set; } = new();
        public List<ConsoleJuriFlow> Flows7d { get; set; } = new();
        public List<ConsoleItemCensus> Items { get; set; } = new();
        public List<ConsoleDuplicate> Duplicates { get; set; } = new();
        public ConsoleShopHealth Shops { get; set; } = new();
    }

    public class ConsoleJuriFlow
    {
        public string Source { get; set; }
        public long JuriIn { get; set; }
        public long JuriOut { get; set; }
        public long Events { get; set; }
    }

    public class ConsoleItemCensus
    {
        public string ItemIDTag { get; set; }
        public long Quantity { get; set; }
        public long Holders { get; set; }
        public int MaxRefine { get; set; }
    }

    public class ConsoleDuplicate
    {
        public Guid InstanceId { get; set; }
        public string ItemIDTag { get; set; }
        public long Copies { get; set; }
        public string Holders { get; set; }
    }

    public class ConsoleShopHealth
    {
        public int OpenShops { get; set; }
        public int Listings { get; set; }
        public int UndeliveredPurchases { get; set; }
        public long UnclaimedProceeds { get; set; }
        public int AbandonedOperations7d { get; set; }
        public List<ConsoleShop> Shops { get; set; } = new();
    }

    public class ConsoleStaffUser
    {
        public Guid UserGUID { get; set; }
        public string FirstName { get; set; }
        public string Role { get; set; }
        public string SteamId { get; set; }
    }

    public class ConsoleMigration
    {
        public string ScriptName { get; set; }
        public DateTime Applied { get; set; }
    }
}
