using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    public partial class UserSessions
    {
        public Guid CustomerGuid { get; set; }
        public Guid UserSessionGuid { get; set; }
        public Guid UserGuid { get; set; }
        public DateTime LoginDate { get; set; }
        public string SelectedCharacterName { get; set; }

        public User UserGu { get; set; }
    }
}
