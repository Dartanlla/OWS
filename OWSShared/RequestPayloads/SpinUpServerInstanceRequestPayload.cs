using System;
using System.Collections.Generic;
using System.Text;

namespace OWSShared.RequestPayloads
{
    public class SpinUpServerInstanceRequestPayload
    {
        public int WorldServerID { get; set; }
        public int MapInstanceID { get; set; }
        public string ZoneName { get; set; }
        public int Port { get; set; }
    }
}
