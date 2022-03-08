using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class GetAbilityBars
    {
        public int CharAbilityBarID { get; set; }
        public string AbilityBarName { get; set; }
        public int CharacterID { get; set; }
        public Guid CustomerGUID { get; set; }
        public int MaxNumberOfSlots { get; set; }
        public int NumberOfUnlockedSlots { get; set; }
        public string CharAbilityBarsCustomJSON { get; set; }
    }
}
