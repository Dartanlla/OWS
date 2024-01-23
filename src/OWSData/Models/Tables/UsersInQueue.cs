using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record UsersInQueue(
        Guid CustomerGuid,
        Guid UserGuid,
        string QueueName,
        DateTime JoinDt,
        int MatchMakingScore
        );
    //public partial class UsersInQueue
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public Guid UserGuid { get; set; }
    //    public string QueueName { get; set; }
    //    public DateTime JoinDt { get; set; }
    //    public int MatchMakingScore { get; set; }
    //}
}
