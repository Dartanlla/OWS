using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace OWSData.Models.StoredProcs
{
    public class GetCharInventoryByCharName
    {
        public GetCharInventoryByCharName()
        {
            
        }
        public string ItemIDTag { get; set; }
        public int Quantity { get; set; }
        public int InSlotNumber { get; set; }
        public string CustomData { get; set; }

    }
}
