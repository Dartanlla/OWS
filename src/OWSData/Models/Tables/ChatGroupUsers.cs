using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class ChatGroupUsers
    {
        public Guid CustomerGuid { get; set; }
        public int ChatGroupId { get; set; }
        public int CharacterId { get; set; }
    }
}
