using System;

namespace OWSData.Models.Composites
{
    /// <summary>
    /// A live MapInstances row as the management console's zone instances grid needs it, with the load its
    /// server last reported.
    /// </summary>
    public class ZoneInstanceSummary : ZoneServerStats
    {
        public int MapInstanceID { get; set; }
        public int WorldServerID { get; set; }
        public string ServerRegion { get; set; }
        public string ZoneName { get; set; }
        public int Port { get; set; }
        /// <summary>1 = starting, 2 = ready, 3 = shutting down.</summary>
        public int Status { get; set; }
        public int NumberOfReportedPlayers { get; set; }
        public DateTime? LastUpdateFromServer { get; set; }
        /// <summary>Comma-separated names of the characters CharOnMapInstance places on this instance.</summary>
        public string OnlineCharacters { get; set; }
    }
}
