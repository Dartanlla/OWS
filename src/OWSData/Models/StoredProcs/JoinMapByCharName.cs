using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSData.Models.StoredProcs
{
    public class JoinMapByCharName
    {
        public string ServerIP { get; set; }
        public string WorldServerIP { get; set; }
        public int WorldServerPort { get; set; }
        public int Port { get; set; }
        public int MapInstanceID { get; set; }
        public string MapNameToStart { get; set; }
        public int WorldServerID { get; set; }
        public int MapInstanceStatus { get; set; }
        public bool NeedToStartupMap { get; set; }
        public bool EnableAutoLoopback { get; set; }
        public bool NoPortForwarding { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
