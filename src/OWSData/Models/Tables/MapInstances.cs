using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record MapInstances(
        Guid CustomerGuid,
        int MapInstanceId,
        int WorldServerId,
        int MapId,
        int Port,
        int? PlayerGroupId,
        int NumberOfReportedPlayers,
        DateTime? LastUpdateFromServer,
        DateTime? LastServerEmptyDate
        )
    {
        public MapInstances(Guid customerGuid,
            int mapInstanceId,
            int worldServerId,
            int mapId,
            int port,
            int? playerGroupId,
            int numberOfReportedPlayers,
            DateTime? lastUpdateFromServer,
            DateTime? lastServerEmptyDate,
            int status) : this(customerGuid, mapInstanceId, worldServerId, mapId, port, playerGroupId, numberOfReportedPlayers, lastUpdateFromServer, lastServerEmptyDate)
        {
            Status = status;
        }

        public int Status { get; set; } = 0;
    }

    //public partial class MapInstances
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public int MapInstanceId { get; set; }
    //    public int WorldServerId { get; set; }
    //    public int MapId { get; set; }
    //    public int Port { get; set; }
    //    public int Status { get; set; } = 0;
    //    public int? PlayerGroupId { get; set; }
    //    public int NumberOfReportedPlayers { get; set; }
    //    public DateTime? LastUpdateFromServer { get; set; }
    //    public DateTime? LastServerEmptyDate { get; set; }
    //}
}
