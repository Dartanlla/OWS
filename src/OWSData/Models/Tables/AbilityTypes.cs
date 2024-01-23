using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record AbilityTypes(int AbilityTypeId, string AbilityTypeName, Guid CustomerGuid)
    {
        public ICollection<Abilities> Abilities { get; init; } = new HashSet<Abilities>();
    }


    //public partial class AbilityTypes
    //{
    //    public AbilityTypes()
    //    {
    //        Abilities = new HashSet<Abilities>();
    //    }

    //    public int AbilityTypeId { get; set; }
    //    public string AbilityTypeName { get; set; }
    //    public Guid CustomerGuid { get; set; }

    //    public ICollection<Abilities> Abilities { get; set; }
    //}
}
