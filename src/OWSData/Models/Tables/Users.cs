using System;
using System.Collections.Generic;

namespace OWSData.Models.Tables
{
    [Serializable]
    public record User(
        Guid UserGuid,
        Guid CustomerGuid,
        string FirstName,
        string LastName,
        string Email,
        string PasswordHash,
        string Salt,
        DateTime CreateDate,
        DateTime LastAccess,
        string Role
        )
    {
        public ICollection<UserSessions> UserSessions { get; set; } = new HashSet<UserSessions>();
    }
    //public partial class User
    //{
    //    public User()
    //    {
    //        UserSessions = new HashSet<UserSessions>();
    //    }

    //    public Guid UserGuid { get; set; }
    //    public Guid CustomerGuid { get; set; }
    //    public string FirstName { get; set; }
    //    public string LastName { get; set; }
    //    public string Email { get; set; }
    //    public string PasswordHash { get; set; }
    //    public string Salt { get; set; }
    //    public DateTime CreateDate { get; set; }
    //    public DateTime LastAccess { get; set; }
    //    public string Role { get; set; }

    //    public ICollection<UserSessions> UserSessions { get; set; }
    //}
}
