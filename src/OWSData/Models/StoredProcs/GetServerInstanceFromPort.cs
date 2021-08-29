using System;
using System.Collections.Generic;
using System.Text;

namespace OWSData.Models.StoredProcs
{
    public class GetServerInstanceFromPort
    {
		private string MapName { get; set; }
		private string ZoneName { get; set; }
		private string WorldCompContainsFilter { get; set; }
		private string WorldCompListFilter { get; set; }
		private int MapInstanceID { get; set; }
		private int Status { get; set; }
		private int MaxNumberOfInstances { get; set; }
		private DateTime? ActiveStartTime { get; set; }
		private byte ServerStatus { get; set; }
		private string InternalServerIP { get; set; }
	}
}
