using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record ChatGroups(
        Guid CustomerGuid,
        int ChatGroupId,
        string ChatGroupName
        );

    //public partial class ChatGroups
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int ChatGroupId { get; set; }
    //    public string ChatGroupName { get; set; }
    //}
}
