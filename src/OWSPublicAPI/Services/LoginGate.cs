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
    /// Decides whether an account may play: an active ban refuses it, and so does maintenance for anyone below
    /// Moderator: ConsoleSettings logins_open=false, or a scheduled maintenance window that has started. The staff
    /// console sets all three.
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

            string maintenance = checkMaintenance ? await MaintenanceRefusal(role) : null;
            return maintenance != null ? ("maintenance", maintenance) : (null, null);
        }

        /// <summary>The console's player cap for entering the world; 0 when off.</summary>
        public async Task<int> PlayerCap()
        {
            return ConsoleSettingValues.PlayerCap(await _settings.GetAll());
        }

        /// <summary>True while logins are closed for maintenance to this role (staff stay allowed).</summary>
        public async Task<bool> ClosedForMaintenance(string role)
        {
            return await MaintenanceRefusal(role) != null;
        }

        /// <summary>The message for a role that maintenance keeps out, or null.</summary>
        private async Task<string> MaintenanceRefusal(string role)
        {
            if (UserRoles.IsStaff(role))
            {
                return null;
            }
            IReadOnlyDictionary<string, string> settings = await _settings.GetAll();
            if (settings.TryGetValue(ConsoleSettingKeys.LoginsOpen, out string open) && string.Equals(open, "false", StringComparison.OrdinalIgnoreCase))
            {
                return MaintenanceMessage;
            }
            MaintenanceWindow window = ConsoleSettingValues.Maintenance(settings);
            return window != null && window.Started(DateTime.UtcNow) ? window.PlayerMessage() : null;
        }
    }
}
