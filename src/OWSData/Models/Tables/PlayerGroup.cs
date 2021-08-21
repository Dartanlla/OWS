using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class PlayerGroup
    {
        public int PlayerGroupId { get; set; }
        public Guid CustomerGuid { get; set; }
        public string PlayerGroupName { get; set; }
        public int PlayerGroupTypeId { get; set; }
        public int ReadyState { get; set; }
        public DateTime? CreateDate { get; set; }

        public PlayerGroupTypes PlayerGroupType { get; set; }
    }
}
