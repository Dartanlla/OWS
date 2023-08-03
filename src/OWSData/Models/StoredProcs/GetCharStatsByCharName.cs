using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace OWSData.Models.StoredProcs
{
    public class GetCharStatsByCharName
    {
        public GetCharStatsByCharName()
        {
            
        }

        public string StatIdentifier { get; set; }
        public int Value { get; set; }

    }
}
