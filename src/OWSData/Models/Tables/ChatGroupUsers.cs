using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record ChatGroupUsers(
        Guid CustomerGuid,
        int ChatGroupId,
        int CharacterId
        );

    //public partial class ChatGroupUsers
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int ChatGroupId { get; set; }
    //    public int CharacterId { get; set; }
    //}
}
