using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class PartyMember
    {
        public string CharName { get; set; }
        public Guid CharacterGUID { get; set; }
        public bool bPartyLeader { get; set; }
        // public DateTime? CreateDate { get; set; }
    }
}
