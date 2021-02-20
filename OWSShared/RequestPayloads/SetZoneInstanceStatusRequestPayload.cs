using System;
using System.Collections.Generic;
using System.Text;

namespace OWSShared.RequestPayloads
{
    public class SetZoneInstanceStatusRequestPayload
    {
        public int MapInstanceID { get; set; }
        public int InstanceStatus { get; set; }
    }
}
