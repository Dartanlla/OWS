using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record CharOnMapInstance(
        Guid CustomerGuid,
        int CharacterId,
        int MapInstanceId
        );

    //public partial class CharOnMapInstance
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int CharacterId { get; set; }
    //    public int MapInstanceId { get; set; }
    //}
}
