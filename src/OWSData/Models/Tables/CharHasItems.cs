using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record CharHasItems(
        Guid CustomerGuid,
        int CharacterId,
        int CharHasItemId,
        int ItemId,
        int Quantity,
        bool Equipped,

        Characters C
        );

    //public partial class CharHasItems
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int CharacterId { get; set; }
    //    public int CharHasItemId { get; set; }
    //    public int ItemId { get; set; }
    //    public int Quantity { get; set; }
    //    public bool Equipped { get; set; }

    //    public Characters C { get; set; }
    //}
}
