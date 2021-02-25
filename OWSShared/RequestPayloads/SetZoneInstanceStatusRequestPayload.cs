using System;
using System.Collections.Generic;
using System.Text;

namespace OWSShared.RequestPayloads
{
    public class SetZoneInstanceStatusRequestPayload
    {
        public int ZoneInstanceID { get; set; }
        public int InstanceStatus { get; set; }
    }
}
