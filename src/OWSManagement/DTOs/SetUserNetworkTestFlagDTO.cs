using System;

namespace OWSManagement.DTOs
{
    public class SetUserNetworkTestFlagDTO
    {
        public Guid UserGUID { get; set; }

        /// <summary>Users.IsInternalNetworkTestUser: applies to every character of this user, new ones included.</summary>
        public bool IsInternalNetworkTestUser { get; set; }
    }
}
