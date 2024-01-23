using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record UserSessions(
        Guid CustomerGuid,
        Guid UserSessionGuid,
        Guid UserGuid,
        DateTime LoginDate,
        string SelectedCharacterName,
        User UserGu
        );
    //public partial class UserSessions
    //{
    //    public Guid CustomerGuid { get; set; }
    //    public Guid UserSessionGuid { get; set; }
    //    public Guid UserGuid { get; set; }
    //    public DateTime LoginDate { get; set; }
    //    public string SelectedCharacterName { get; set; }

    //    public User UserGu { get; set; }
    //}
}
