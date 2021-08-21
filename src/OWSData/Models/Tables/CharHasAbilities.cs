using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class CharHasAbilities
    {
        public CharHasAbilities()
        {
            CharAbilityBarAbilities = new HashSet<CharAbilityBarAbilities>();
        }

        public Guid CustomerGuid { get; set; }
        public int CharHasAbilitiesId { get; set; }
        public int CharacterId { get; set; }
        public int AbilityId { get; set; }
        public int AbilityLevel { get; set; }
        public string CharHasAbilitiesCustomJson { get; set; }

        public Characters C { get; set; }
        public ICollection<CharAbilityBarAbilities> CharAbilityBarAbilities { get; set; }
    }
}
