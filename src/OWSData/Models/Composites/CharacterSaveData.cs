using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSData.Models.Composites
{
    public class CharacterSaveData
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public string CharGuid { get; set; }

        public IEnumerable<CharacterStat> CharStats { get; set; }

        public IEnumerable<CharacterQuest> CharQuests { get; set; }

        public IEnumerable<CharacterInventory> CharInventory { get; set; }

        public IEnumerable<CharacterCurrency> CharCurrency { get; set; }
    }


    public class CharacterStat
    {
        public string StatIdentifier { get; set; }
        public int Value { get; set; }
    }

    public class CharacterQuest
    {
        public string QuestIDTag { get; set; }
        public string QuestJournalTagContainer { get; set; }
        public string QuestClassName { get; set; }
        public string CustomData { get; set; }
    }


    public class CharacterInventory
    {
        public string ItemIDTag { get; set; }
        public int Quantity { get; set; }
        public int InSlotNumber { get; set; }
        public string CustomData { get; set; }
    }

    public class CharacterCurrency
    {

    }
}
