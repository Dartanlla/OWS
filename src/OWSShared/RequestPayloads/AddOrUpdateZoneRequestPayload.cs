using System;
using System.Collections.Generic;
using System.Text;

namespace OWSShared.RequestPayloads
{
    public class AddOrUpdateZoneRequestPayload
    {
        public Guid CustomerGUID { get; set; }
		public int MapID  { get; set; }
		public string MapName { get; set; }
		public string ZoneName { get; set; }
		public string WorldCompContainsFilter { get; set; }
		public string WorldCompListFilter { get; set; }
		public int SoftPlayerCap { get; set; }
		public int HardPlayerCap { get; set; }
		public int MapMode { get; set; }
	}
}
