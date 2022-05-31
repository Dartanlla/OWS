using System;

namespace OWSData.Models.Tables
{
    public partial class Abilities
    {
        public Guid CustomerGuid { get; set; }
        public int AbilityId { get; set; }
        public string AbilityName { get; set; }
        public int AbilityTypeId { get; set; }
        public string TextureToUseForIcon { get; set; }
        public int? Class { get; set; }
        public int? Race { get; set; }
        public string AbilityCustomJson { get; set; }
        public string GameplayAbilityClassName { get; set; }
        public string AbilityTypeName { get; set; }
    }
}
