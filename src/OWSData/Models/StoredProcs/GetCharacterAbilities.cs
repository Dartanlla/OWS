using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class GetCharacterAbilities
    {
        public int AbilityID { get; set; } 
        public string AbilityCustomJSON { get; set; } 
        public string AbilityName { get; set; }
        public int AbilityTypeID { get; set; }
        public string Class { get; set; }
        public Guid CustomerGUID { get; set; }
        public string Race { get; set; }
        public string TextureToUseForIcon { get; set; }
        public string GameplayAbilityClassName { get; set; }
        public int CharHasAbilitiesID { get; set; }
        public int AbilityLevel { get; set; }
        public string CharHasAbilitiesCustomJSON { get; set; }
        public int CharacterID { get; set; }
        public string CharName { get; set; }
    }
}
