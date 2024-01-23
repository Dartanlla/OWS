using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record DebugLog(
        int DebugLogId,
        DateTime DebugDate,
        string DebugDesc,
        Guid CustomerGuid
        );

    //public partial class DebugLog
    //{
    //    public int DebugLogId { get; set; }
    //    public DateTime DebugDate { get; set; }
    //    public string DebugDesc { get; set; }
    //    public Guid CustomerGuid { get; set; }
    //}
}
