using System;

namespace OWSData.Models.Composites
{
    /// <summary>
    /// A user as the management console grid needs it: the Users columns worth showing, plus
    /// a roll-up of the network-test flag, which lives on Characters rather than Users.
    /// </summary>
    public class AdminUserSummary
    {
        public Guid UserGUID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string SteamId { get; set; }
        public string Role { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastAccess { get; set; }

        /// <summary>Characters owned by this user.</summary>
        public int CharacterCount { get; set; }

        /// <summary>How many of them have Characters.IsInternalNetworkTestUser set.</summary>
        public int NetworkTestCharacterCount { get; set; }
    }
}
