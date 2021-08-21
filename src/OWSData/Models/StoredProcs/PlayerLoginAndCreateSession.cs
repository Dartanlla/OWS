using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSData.Models.StoredProcs
{
    public class PlayerLoginAndCreateSession
    {
        public bool Authenticated { get; set; }
        public Guid? UserSessionGuid { get; set; }
        public string ErrorMessage { get; set; }

        public PlayerLoginAndCreateSession()
        {
            ErrorMessage = "";
        }
    }
}
