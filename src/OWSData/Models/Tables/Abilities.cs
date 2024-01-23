using System;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record Abilities(
        Guid CustomerGuid,
        int AbilityId,
        string AbilityName,
        int AbilityTypeId,
        string TextureToUseForIcon,
        int? Class,
        int? Race,
        string AbilityCustomJson,
        string GameplayAbilityClassName,
        string AbilityTypeName
        );

    //public partial class Abilities
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int AbilityId { get; set; }
    //    public string AbilityName { get; set; }
    //    public int AbilityTypeId { get; set; }
    //    public string TextureToUseForIcon { get; set; }
    //    public int? Class { get; set; }
    //    public int? Race { get; set; }
    //    public string AbilityCustomJson { get; set; }
    //    public string GameplayAbilityClassName { get; set; }
    //    public string AbilityTypeName { get; set; }
    //}
}
