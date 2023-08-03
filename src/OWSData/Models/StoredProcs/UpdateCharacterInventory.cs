using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class UpdateCharacterInventory
    {

        public string ItemIDTag { get; set; }

        public int Quantity { get; set; }

        public int InSlotNumber { get; set; }

        public string CustomData { get; set; }
    }
}
