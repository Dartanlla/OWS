using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class CharInventoryItems
    {
        public Guid CustomerGuid { get; set; }
        public int CharInventoryId { get; set; }
        public int CharInventoryItemId { get; set; }
        public int ItemId { get; set; }
        public int InSlotNumber { get; set; }
        public int Quantity { get; set; }
        public int NumberOfUsesLeft { get; set; }
        public int Condition { get; set; }
        public Guid CharInventoryItemGuid { get; set; }
    }
}
