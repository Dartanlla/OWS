using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record ChatMessages(
        Guid CustomerGuid,
        int ChatMessageId,
        int SentByCharId,
        int? SentToCharId,
        int? ChatGroupId,
        string ChatMessage,
        DateTime MessageDate
        );

    //public partial class ChatMessages
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int ChatMessageId { get; set; }
    //    public int SentByCharId { get; set; }
    //    public int? SentToCharId { get; set; }
    //    public int? ChatGroupId { get; set; }
    //    public string ChatMessage { get; set; }
    //    public DateTime MessageDate { get; set; }
    //}
}
