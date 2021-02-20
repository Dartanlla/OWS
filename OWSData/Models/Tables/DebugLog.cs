using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class DebugLog
    {
        public int DebugLogId { get; set; }
        public DateTime DebugDate { get; set; }
        public string DebugDesc { get; set; }
        public Guid CustomerGuid { get; set; }
    }
}
