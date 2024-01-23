using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record CharInventoryItems(
        Guid CustomerGuid,
        int CharInventoryId,
        int CharInventoryItemId,
        int ItemId,
        int InSlotNumber,
        int Quantity,
        int NumberOfUsesLeft,
        int Condition,
        Guid CharInventoryItemGuid
        );

    //public partial class CharInventoryItems
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int CharInventoryId { get; set; }
    //    public int CharInventoryItemId { get; set; }
    //    public int ItemId { get; set; }
    //    public int InSlotNumber { get; set; }
    //    public int Quantity { get; set; }
    //    public int NumberOfUsesLeft { get; set; }
    //    public int Condition { get; set; }
    //    public Guid CharInventoryItemGuid { get; set; }
    //}
}
