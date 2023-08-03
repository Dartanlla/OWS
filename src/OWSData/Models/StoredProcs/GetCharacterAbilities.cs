using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class GetCharacterAbilities
    {
        public string AbilityIDTag { get; set; }
        public int CurrentAbilityLevel { get; set; }
        public int ActualAbilityLevel { get; set; }
        public string CustomData { get; set; } 
    }
}
