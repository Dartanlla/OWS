using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models
{
    public class OWSInstanceLauncherOptions
    {
        public const string SectionName = "OWSInstanceLauncherOptions";
        public string OWSAPIKey { get; set; }
        public string LauncherGuid { get; set; }
        public string ServerIP { get; set; }
        public int MaxNumberOfInstances { get; set; }
        public string InternalServerIP { get; set; }
        public int StartingInstancePort { get; set; }
        public string PathToDedicatedServer { get; set; }
        public int RunServerHealthMonitoringFrequencyInSeconds { get; set; }
        public string ServerArguments { get; set; }
        
    }
}
