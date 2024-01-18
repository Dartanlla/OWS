using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record ItemTypes(
        Guid CustomerGuid,
        int ItemTypeId,
        string ItemTypeDesc,
        short UserItemType,
        short EquipmentType,
        short ItemQuality,
        short EquipmentSlotType
        );

    //public partial class ItemTypes
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int ItemTypeId { get; set; }
    //    public string ItemTypeDesc { get; set; }
    //    public short UserItemType { get; set; }
    //    public short EquipmentType { get; set; }
    //    public short ItemQuality { get; set; }
    //    public short EquipmentSlotType { get; set; }
    //}
}
