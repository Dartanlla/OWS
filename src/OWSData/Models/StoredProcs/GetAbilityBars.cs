using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    [Serializable]
    public record GetAbilityBars(
        int CharAbilityBarID,
        string AbilityBarName,
        int CharacterID,
        Guid CustomerGUID,
        int MaxNumberOfSlots,
        int NumberOfUnlockedSlots,
        string CharAbilityBarsCustomJSON
        );

    //public class GetAbilityBars
    //{
    //    public int CharAbilityBarID { get; set; }
    //    public string AbilityBarName { get; set; }
    //    public int CharacterID { get; set; }
    //    public Guid CustomerGUID { get; set; }
    //    public int MaxNumberOfSlots { get; set; }
    //    public int NumberOfUnlockedSlots { get; set; }
    //    public string CharAbilityBarsCustomJSON { get; set; }
    //}
}
