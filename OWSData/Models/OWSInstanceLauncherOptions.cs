using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models
{
    public class OWSInstanceLauncherOptions
    {
        public string OWSAPIKey { get; set; }
        public string PathToDedicatedServer { get; set; }
        public string ServerArguments { get; set; }
    }
}
