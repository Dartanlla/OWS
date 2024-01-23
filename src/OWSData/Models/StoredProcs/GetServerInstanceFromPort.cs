using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OWSData.Models.StoredProcs
{
	[Serializable]
    public record GetServerInstanceFromPort
		(
        string MapName,
        string ZoneName,
        string WorldCompContainsFilter,
        string WorldCompListFilter,
        int MapInstanceID,
        int Status,
        int MaxNumberOfInstances,
        DateTime? ActiveStartTime,
        byte ServerStatus,
        IPAddress InternalServerIP
        );

    //   public class GetServerInstanceFromPort
    //   {
    //	public string MapName { get; set; }
    //	public string ZoneName { get; set; }
    //	public string WorldCompContainsFilter { get; set; }
    //	public string WorldCompListFilter { get; set; }
    //	public int MapInstanceID { get; set; }
    //	public int Status { get; set; }
    //	public int MaxNumberOfInstances { get; set; }
    //	public DateTime? ActiveStartTime { get; set; }
    //	public byte ServerStatus { get; set; }
    //	public string InternalServerIP { get; set; }
    //}
}
