using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class ChatMessages
    {
        public Guid CustomerGuid { get; set; }
        public int ChatMessageId { get; set; }
        public int SentByCharId { get; set; }
        public int? SentToCharId { get; set; }
        public int? ChatGroupId { get; set; }
        public string ChatMessage { get; set; }
        public DateTime MessageDate { get; set; }
    }
}
