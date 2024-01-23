﻿using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record CharAbilityBars(
        Guid CustomerGuid,
        int CharAbilityBarId,
        int CharacterId,
        string AbilityBarName,
        string CharAbilityBarsCustomJson,
        int MaxNumberOfSlots,
        int NumberOfUnlockedSlots
        )
    {
        public ICollection<CharAbilityBarAbilities> CharAbilityBarAbilities { get; set; } = new HashSet<CharAbilityBarAbilities>();
    }

    //public partial class CharAbilityBars
    //{
    //    public CharAbilityBars()
    //    {
    //        CharAbilityBarAbilities = new HashSet<CharAbilityBarAbilities>();
    //    }

    //    public Guid CustomerGuid { get; set; }
    //    public int CharAbilityBarId { get; set; }
    //    public int CharacterId { get; set; }
    //    public string AbilityBarName { get; set; }
    //    public string CharAbilityBarsCustomJson { get; set; }
    //    public int MaxNumberOfSlots { get; set; }
    //    public int NumberOfUnlockedSlots { get; set; }

    //    public ICollection<CharAbilityBarAbilities> CharAbilityBarAbilities { get; set; }
    //}
}


