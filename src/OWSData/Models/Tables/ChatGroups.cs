using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class ChatGroups
    {
        public Guid CustomerGuid { get; set; }
        public int ChatGroupId { get; set; }
        public string ChatGroupName { get; set; }
    }
}
