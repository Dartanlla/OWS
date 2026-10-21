namespace OWSData.Models.Composites
{
    /// <summary>
    /// The load a zone server reports in its UpdateNumberOfPlayers heartbeat. Every field is null when the
    /// server's build predates them.
    /// </summary>
    public class ZoneServerStats
    {
        /// <summary>World-tick mean over the heartbeat interval, in ms.</summary>
        public float? ServerFrameMsAvg { get; set; }
        /// <summary>Worst single world tick over the heartbeat interval, in ms.</summary>
        public float? ServerFrameMsMax { get; set; }
        public int? MemoryUsedMB { get; set; }
        public int? UptimeSeconds { get; set; }
        /// <summary>The server build's changelist; 0 is an unstamped dev build.</summary>
        public int? BuildCL { get; set; }
    }
}
