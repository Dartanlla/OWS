using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.Services;
using OWSShared.Interfaces;
using Serilog;

namespace OWSPublicAPI.Requests.Users
{
    /// <summary>
    /// SteamLoginAndCreateSessionRequest Handler
    /// </summary>
    /// <remarks>
    /// Handles api/Users/SteamLoginAndCreateSession requests. The client obtains a WebAPI auth
    /// ticket via ISteamUser::GetAuthTicketForWebApi and we validate it with Valve, JIT-creating
    /// a user for unknown SteamIds.
    /// </remarks>
    public class SteamLoginAndCreateSessionRequest : IRequestHandler<SteamLoginAndCreateSessionRequest, IActionResult>, IRequest
    {
        /// <summary>
        /// Hex-encoded WebAPI auth ticket from ISteamUser::GetAuthTicketForWebApi.
        /// </summary>
        public string SteamAuthTicket { get; set; }

        private PlayerLoginAndCreateSession output;
        private Guid customerGUID;
        private IUsersRepository usersRepository;
        private ISteamAuthService steamAuthService;
        private IAuditRepository auditRepository;
        private LoginGate loginGate;

        public void SetData(IUsersRepository usersRepository, ISteamAuthService steamAuthService, IHeaderCustomerGUID customerGuid, IAuditRepository auditRepository,
            LoginGate loginGate)
        {
            this.loginGate = loginGate;
            this.customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
            this.steamAuthService = steamAuthService;
            this.auditRepository = auditRepository;
        }

        public async Task<IActionResult> Handle()
        {
            SteamTicketValidationResult validation = await steamAuthService.ValidateAuthTicket(SteamAuthTicket);

            if (!validation.IsValid)
            {
                return await Unauthenticated("steam_rejected", validation.SteamId, string.IsNullOrWhiteSpace(validation.ErrorMessage) ? "Steam authentication failed" : validation.ErrorMessage);
            }

            if (validation.VacBanned || validation.PublisherBanned)
            {
                return await Unauthenticated("banned", validation.SteamId, "This Steam account is banned");
            }

            string personaName = await steamAuthService.GetPersonaName(validation.SteamId) ?? "SteamUser";
            // FirstName column is VARCHAR(50).
            if (personaName.Length > 50)
            {
                personaName = personaName.Substring(0, 50);
            }

            output = await usersRepository.SteamLoginAndCreateSession(customerGUID, validation.SteamId, personaName);

            if (output == null)
            {
                return await Unauthenticated("unavailable", validation.SteamId, "Login service unavailable");
            }

            bool loggedIn = output.Authenticated && output.UserSessionGuid.HasValue && output.UserSessionGuid != Guid.Empty;
            if (!loggedIn && string.IsNullOrWhiteSpace(output.ErrorMessage))
            {
                output.ErrorMessage = "Steam authentication failed";
            }

            if (loggedIn)
            {
                (string refusal, string message) = await loginGate.RefusalForSession(customerGUID, output.UserSessionGuid.Value, checkMaintenance: true);
                if (refusal != null)
                {
                    await RecordUserLogin(refusal, validation.SteamId, personaName, null);
                    return new OkObjectResult(new PlayerLoginAndCreateSession { Authenticated = false, UserSessionGuid = Guid.Empty, ErrorMessage = message });
                }
            }

            await RecordUserLogin(loggedIn ? "ok" : "refused", validation.SteamId, personaName, output.UserSessionGuid);
            return new OkObjectResult(output);
        }

        private async Task RecordUserLogin(string result, string steamId, string personaName, Guid? userSessionGuid)
        {
            Log.Information("user_login {Method} {Result} steam {SteamId} {PersonaName} session {UserSessionGuid}",
                "steam", result, steamId, personaName, userSessionGuid);
            await auditRepository.Add(customerGUID, "publicapi", personaName ?? steamId, "user_login", "steam:" + steamId,
                new { method = "steam", result, userSessionGuid });
        }

        private async Task<IActionResult> Unauthenticated(string result, string steamId, string errorMessage)
        {
            await RecordUserLogin(result, steamId, null, null);
            return new OkObjectResult(new PlayerLoginAndCreateSession
            {
                Authenticated = false,
                UserSessionGuid = Guid.Empty,
                ErrorMessage = errorMessage
            });
        }
    }
}
