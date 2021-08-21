using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class AbilityTypes
    {
        public AbilityTypes()
        {
            Abilities = new HashSet<Abilities>();
        }

        public int AbilityTypeId { get; set; }
        public string AbilityTypeName { get; set; }
        public Guid CustomerGuid { get; set; }

        public ICollection<Abilities> Abilities { get; set; }
    }
}
