using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OWSData.Models.StoredProcs
{
    
    public class GetPartyInformation
    {
        public GetPartyInformation()
        {

        }

        public Guid PartyGUID { get; set; }
        public bool bRaidingParty { get; set; }
        public IEnumerable<GetPartyMemberInformation> PartyMembers { get; set; }
          
    }

    public class GetPartyMemberInformation
    {
        public Guid CharacterGUID { get; set; }

        public string CharName { get; set;}

        public bool bPartyLeader { get; set;}
    }
}
