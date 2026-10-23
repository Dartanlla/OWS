using System;
using System.Collections.Generic;
using System.Linq;

namespace OWSData.Models
{
    /// <summary>
    /// The Users.Role values the AddUser stored procedure recognises. The column is
    /// VARCHAR(10), and the procedure passes anything it does not recognise straight
    /// through, so callers must normalise against this list before writing.
    /// </summary>
    public static class UserRoles
    {
        public const string Player = "Player";
        public const string Moderator = "Moderator";
        public const string GameMaster = "GameMaster";
        public const string Admin = "Admin";

        public static readonly IReadOnlyList<string> All = new[] { Player, Moderator, GameMaster, Admin };

        /// <summary>
        /// Maps free-form input onto a known role. Accepts the short aliases the stored
        /// procedure understands ("mod", "gm"). Returns false for anything else rather
        /// than silently downgrading to Player.
        /// </summary>
        public static bool TryNormalize(string role, out string normalized)
        {
            normalized = null;

            if (string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            switch (role.Trim().ToLowerInvariant())
            {
                case "player":
                    normalized = Player;
                    return true;
                case "mod":
                case "moderator":
                    normalized = Moderator;
                    return true;
                case "gm":
                case "gamemaster":
                    normalized = GameMaster;
                    return true;
                case "admin":
                    normalized = Admin;
                    return true;
                default:
                    return false;
            }
        }

        public static string NormalizeOrDefault(string role)
        {
            return TryNormalize(role, out string normalized) ? normalized : Player;
        }

        /// <summary>Player 0, Moderator 1, GameMaster 2, Admin 3; anything unknown ranks as Player.</summary>
        public static int Rank(string role)
        {
            return NormalizeOrDefault(role) switch
            {
                Moderator => 1,
                GameMaster => 2,
                Admin => 3,
                _ => 0
            };
        }

        public static bool IsStaff(string role) => Rank(role) >= 1;
    }
}
