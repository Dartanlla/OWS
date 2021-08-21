using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models
{
    public class ZoneServerProcess
    {
        public int ZoneInstanceId { get; set; }
        public string MapName { get; set; }
        public int Port { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }

    }
}
