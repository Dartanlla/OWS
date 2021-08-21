using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class CharInventory
    {
        public Guid CustomerGuid { get; set; }
        public int CharacterId { get; set; }
        public int CharInventoryId { get; set; }
        public string InventoryName { get; set; }
        public int InventorySize { get; set; }
    }
}
