using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class MapInstances
    {
        public Guid CustomerGuid { get; set; }
        public int MapInstanceId { get; set; }
        public int WorldServerId { get; set; }
        public int MapId { get; set; }
        public int Port { get; set; }
        public int Status { get; set; } = 0;
        public int? PlayerGroupId { get; set; }
        public int NumberOfReportedPlayers { get; set; }
        public DateTime? LastUpdateFromServer { get; set; }
        public DateTime? LastServerEmptyDate { get; set; }
    }
}
