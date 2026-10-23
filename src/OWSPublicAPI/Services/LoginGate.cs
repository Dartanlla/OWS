using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;

namespace OWSPublicAPI.Services
{
    /// <summary>
    /// Decides whether an account may play: an active ban refuses it, and so does maintenance (ConsoleSettings
    /// logins_open=false) for anyone below Moderator. The staff console sets both.
    /// </summary>
    public class LoginGate
    {
        public const string MaintenanceMessage = "The servers are closed for maintenance. Try again later.";

        private readonly ISanctionsRepository _sanctions;
        private readonly IConsoleSettingsRepository _settings;
        private readonly IUsersRepository _users;

        public LoginGate(ISanctionsRepository sanctions, IConsoleSettingsRepository settings, IUsersRepository users)
        {
            _sanctions = sanctions;
            _settings = settings;
            _users = users;
        }

        /// <summary>The refusal for this session's account, or null. A session that does not resolve is left to the caller's own checks.</summary>
        public async Task<(string Result, string Message)> RefusalForSession(Guid customerGUID, Guid userSessionGUID, bool checkMaintenance)
        {
            GetUserSession session = await _users.GetUserSession(customerGUID, userSessionGUID);
            if (session == null || !session.UserGuid.HasValue)
            {
                return (null, null);
            }
            return await RefusalForUser(customerGUID, session.UserGuid.Value, session.Role, checkMaintenance);
        }

        public async Task<(string Result, string Message)> RefusalForUser(Guid customerGUID, Guid userGUID, string role, bool checkMaintenance)
        {
            ActiveSanctions active = await _sanctions.GetActive(customerGUID, userGUID);
            if (active.Ban != null)
            {
                return ("banned", active.Ban.PlayerMessage());
            }

            if (checkMaintenance && !UserRoles.IsStaff(role))
            {
                IReadOnlyDictionary<string, string> settings = await _settings.GetAll();
                if (settings.TryGetValue(ConsoleSettingKeys.LoginsOpen, out string open)
                    && string.Equals(open, "false", StringComparison.OrdinalIgnoreCase))
                {
                    return ("maintenance", MaintenanceMessage);
                }
            }
            return (null, null);
        }
    }
}
