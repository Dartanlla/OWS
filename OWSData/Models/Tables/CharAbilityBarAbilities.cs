using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class CharAbilityBarAbilities
    {
        public Guid CustomerGuid { get; set; }
        public int CharAbilityBarAbilityId { get; set; }
        public int CharAbilityBarId { get; set; }
        public int CharHasAbilitiesId { get; set; }
        public string CharAbilityBarAbilitiesCustomJson { get; set; }
        public int InSlotNumber { get; set; }

        public CharAbilityBars C { get; set; }
        public CharHasAbilities CNavigation { get; set; }
    }
}
