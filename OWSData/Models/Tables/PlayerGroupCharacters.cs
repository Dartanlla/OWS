using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class PlayerGroupCharacters
    {
        public int PlayerGroupId { get; set; }
        public Guid CustomerGuid { get; set; }
        public int CharacterId { get; set; }
        public DateTime DateAdded { get; set; }
        public int TeamNumber { get; set; }
    }
}
