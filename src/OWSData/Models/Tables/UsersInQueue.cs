using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class UsersInQueue
    {
        public Guid CustomerGuid { get; set; }
        public Guid UserGuid { get; set; }
        public string QueueName { get; set; }
        public DateTime JoinDt { get; set; }
        public int MatchMakingScore { get; set; }
    }
}
