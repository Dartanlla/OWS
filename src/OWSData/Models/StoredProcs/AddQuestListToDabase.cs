using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class AddQuestListToDabase
    {
        public string QuestIDTag { get; set; }
        public string QuestOverview { get; set; }
        public string QuestTasks { get; set; }
        public string QuestClassName { get; set; }
        public string CustomData { get; set; }
    }
}