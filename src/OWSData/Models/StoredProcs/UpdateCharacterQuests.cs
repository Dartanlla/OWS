using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class UpdateCharacterQuest
    {
        public string QuestIDTag { get; set; }

        public string QuestJournalTagContainer { get; set; }

        public string CustomData { get; set; }

        public string QuestClassName { get; set; }
    }
}
