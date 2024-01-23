using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record PlayerGroupTypes(
        int PlayerGroupTypeId,
        string PlayerGroupTypeDesc
        )
    {
        public ICollection<PlayerGroup> PlayerGroup { get; set; } = new HashSet<PlayerGroup>();
    }

    //public partial class PlayerGroupTypes
    //{
    //    public PlayerGroupTypes()
    //    {
    //        PlayerGroup = new HashSet<PlayerGroup>();
    //    }

    //    public int PlayerGroupTypeId { get; set; }
    //    public string PlayerGroupTypeDesc { get; set; }

    //    public ICollection<PlayerGroup> PlayerGroup { get; set; }
    //}
}
