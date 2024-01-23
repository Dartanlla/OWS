using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    [Serializable]
    public record GetAbilityBarsAndAbilities
    (
     string AbilityBarName,
     int CharAbilityBarID,
     string CharAbilityBarsCustomJSON,
     int CharacterID,
     Guid CustomerGUID,
     int MaxNumberOfSlots,
     int NumberOfUnlockedSlots,

     IEnumerable<GetCharacterAbilities> Abilities
    );

    //public class GetAbilityBarsAndAbilities
    //{
    //    public string AbilityBarName { get; set; }
    //    public int CharAbilityBarID { get; set; }
    //    public string CharAbilityBarsCustomJSON { get; set; }
    //    public int CharacterID { get; set; }
    //    public Guid CustomerGUID { get; set; }
    //    public int MaxNumberOfSlots { get; set; }
    //    public int NumberOfUnlockedSlots { get; set; }

    //    public IEnumerable<GetCharacterAbilities> Abilities { get; set; }
    //}

    public record AbilityBarsAndAbilities
        (
         string AbilityBarName,
         int CharAbilityBarID,
         string CharAbilityBarsCustomJSON,
         int CharacterID,
         Guid CustomerGUID,
         int MaxNumberOfSlots,
         int NumberOfUnlockedSlots,

         int AbilityLevel,
         string CharHasAbilitiesCustomJSON,

         int AbilityID,
         string AbilityName,
         int AbilityTypeID,
         string Class,
         string TextureToUseForIcon,
         string GameplayAbilityClassName,
         string AbilityCustomJSON,
         int InSlotNumber
        );

    //public class AbilityBarsAndAbilities
    //{
    //    public string AbilityBarName { get; set; }
    //    public int CharAbilityBarID { get; set; }
    //    public string CharAbilityBarsCustomJSON { get; set; }
    //    public int CharacterID { get; set; }
    //    public Guid CustomerGUID { get; set; }
    //    public int MaxNumberOfSlots { get; set; }
    //    public int NumberOfUnlockedSlots { get; set; }

    //    public int AbilityLevel { get; set; }
    //    public string CharHasAbilitiesCustomJSON { get; set; }

    //    public int AbilityID { get; set; }
    //    public string AbilityName { get; set; }
    //    public int AbilityTypeID { get; set; }
    //    public string Class { get; set; }
    //    public string TextureToUseForIcon { get; set; }
    //    public string GameplayAbilityClassName { get; set; }
    //    public string AbilityCustomJSON { get; set; }
    //    public int InSlotNumber { get; set; }
    //}
}
