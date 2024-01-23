using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record CharAbilityBarAbilities(
        Guid CustomerGuid,
        int CharAbilityBarAbilityId,
        int CharAbilityBarId,
        int CharHasAbilitiesId,
        string CharAbilityBarAbilitiesCustomJson,
        int InSlotNumber,

        CharAbilityBars C,
        CharHasAbilities CNavigation
        );

    //public partial class CharAbilityBarAbilities
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int CharAbilityBarAbilityId { get; set; }
    //    public int CharAbilityBarId { get; set; }
    //    public int CharHasAbilitiesId { get; set; }
    //    public string CharAbilityBarAbilitiesCustomJson { get; set; }
    //    public int InSlotNumber { get; set; }

    //    public CharAbilityBars C { get; set; }
    //    public CharHasAbilities CNavigation { get; set; }
    //}
}
