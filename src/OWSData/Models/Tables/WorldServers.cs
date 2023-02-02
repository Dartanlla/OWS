using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class WorldServers
    {
        public Guid CustomerGuid { get; set; }
        public int WorldServerId { get; set; }
        public string ServerIp { get; set; }
        public int MaxNumberOfInstances { get; set; }
        public DateTime? ActiveStartTime { get; set; }
        public int Port { get; set; }
        public byte ServerStatus { get; set; }
        public string InternalServerIp { get; set; }
        public int StartingMapInstancePort { get; set; }
    }
}
