using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.Tables;

namespace OWSData.Models.StoredProcs
{
    public class GetUserSessionComposite
    {
        public UserSessions userSession { get; set; }
        public User user { get; set; }
        public Characters character { get; set; }

    }
    public class GetUserSession
    {
        public Guid CustomerGuid { get; set; }
        public Guid? UserGuid { get; set; }
        public Guid UserSessionGUID { get; set; }
        public DateTime LoginDate { get; set; }
        public string SelectedCharacterName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastAccess { get; set; }
        public string Role { get; set; }

        public int CharacterID { get; set; }
        public string CharName { get; set; }
        public string ZoneName { get; set; }
        public string ZoneNameTag { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Rx { get; set; }
        public double Ry { get; set; }
        public double Rz { get; set; }        
    }
}
