using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OWSData.Models.StoredProcs
{
    
    public class CreateParty
    {
        public CreateParty()
        {

        }

        public Guid CharacterGuid { get; set; }

        public string CharacterName { get; set; }
        public bool PartyLead { get; set; }
        
        
    
    }
}
