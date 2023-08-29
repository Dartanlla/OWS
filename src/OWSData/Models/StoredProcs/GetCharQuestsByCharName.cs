using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace OWSData.Models.StoredProcs
{
    public class GetCharQuestsByCharName
    {
        public GetCharQuestsByCharName()
        {
            
        }
        public string QuestIDTag { get; set; }
        public string QuestJournalTagContainer { get; set; }
        public string QuestClassName { get; set; }
        public string CustomData { get; set; }

    }
}
