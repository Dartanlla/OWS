using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class Party
    {
        public Guid CustomerGuid { get; set; }
        public int PartyID { get; set; }
        public Guid PartyGuid { get; set; }
        public bool bRaidingParty { get; set; }
       // public DateTime? CreateDate { get; set; }
    }
}
