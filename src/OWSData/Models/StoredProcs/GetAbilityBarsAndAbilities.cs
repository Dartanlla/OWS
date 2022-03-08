using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class GetAbilityBarsAndAbilities
    {
        public string AbilityBarName { get; set; }
        public int CharAbilityBarID { get; set; }
        public string CharAbilityBarsCustomJSON { get; set; }
        public int CharacterID { get; set; }
        public Guid CustomerGUID { get; set; }
        public int MaxNumberOfSlots { get; set; }
        public int NumberOfUnlockedSlots { get; set; }

        public IEnumerable<GetCharacterAbilities> Abilities { get; set; }

    }

    public class AbilityBarsAndAbilities
    {
        public string AbilityBarName { get; set; }
        public int CharAbilityBarID { get; set; }
        public string CharAbilityBarsCustomJSON { get; set; }
        public int CharacterID { get; set; }
        public Guid CustomerGUID { get; set; }
        public int MaxNumberOfSlots { get; set; }
        public int NumberOfUnlockedSlots { get; set; }

        public int AbilityLevel { get; set; }
        public string CharHasAbilitiesCustomJSON { get; set; }

        public int AbilityID { get; set; }
        public string AbilityName { get; set; }
        public int AbilityTypeID { get; set; }
        public string Class { get; set; }
        public string TextureToUseForIcon { get; set; }
        public string GameplayAbilityClassName { get; set; }
        public string AbilityCustomJSON { get; set; }
        public int InSlotNumber { get; set; }
    }
}
