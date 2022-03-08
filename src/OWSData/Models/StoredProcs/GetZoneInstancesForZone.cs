using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class GetZoneInstancesForZone
    {
        public int MapID { get; set; }
        public string MapName { get; set; }
        public string ZoneName { get; set; }
        public string WorldCompContainsFilter { get; set; }
        public string WorldCompListFilter { get; set; }
        public string MapMode { get; set; }
        public int SoftPlayerCap { get; set; }
        public int HardPlayerCap { get; set; }
        public int MinutesToShutdownAfterEmpty { get; set; }

        public int MapInstanceID { get; set; }
        public int WorldServerID { get; set; }
        public int Port { get; set; }
        public int Status { get; set; }
        public int PlayerGroupID { get; set; }
        public int NumberOfReportedPlayers { get; set; }
        public DateTime? LastUpdateFromServer { get; set; }
        public DateTime? LastServerEmptyDate { get; set; }
        public DateTime? CreateDate { get; set; }
    }
}
