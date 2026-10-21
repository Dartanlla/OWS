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

        public void SetData(IUsersRepository usersRepository, ISteamAuthService steamAuthService, IHeaderCustomerGUID customerGuid)
        {
            this.customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
            this.steamAuthService = steamAuthService;
        }

        public async Task<IActionResult> Handle()
        {
            SteamTicketValidationResult validation = await steamAuthService.ValidateAuthTicket(SteamAuthTicket);

            if (!validation.IsValid)
            {
                return Unauthenticated("steam_rejected", validation.SteamId, string.IsNullOrWhiteSpace(validation.ErrorMessage) ? "Steam authentication failed" : validation.ErrorMessage);
            }

            if (validation.VacBanned || validation.PublisherBanned)
            {
                return Unauthenticated("banned", validation.SteamId, "This Steam account is banned");
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
                return Unauthenticated("unavailable", validation.SteamId, "Login service unavailable");
            }

            bool loggedIn = output.Authenticated && output.UserSessionGuid.HasValue && output.UserSessionGuid != Guid.Empty;
            if (!loggedIn && string.IsNullOrWhiteSpace(output.ErrorMessage))
            {
                output.ErrorMessage = "Steam authentication failed";
            }

            LogUserLogin(loggedIn ? "ok" : "refused", validation.SteamId, personaName, output.UserSessionGuid);
            return new OkObjectResult(output);
        }

        private static void LogUserLogin(string result, string steamId, string personaName, Guid? userSessionGuid)
        {
            Log.Information("user_login {Method} {Result} steam {SteamId} {PersonaName} session {UserSessionGuid}",
                "steam", result, steamId, personaName, userSessionGuid);
        }

        private IActionResult Unauthenticated(string result, string steamId, string errorMessage)
        {
            LogUserLogin(result, steamId, null, null);
            return new OkObjectResult(new PlayerLoginAndCreateSession
            {
                Authenticated = false,
                UserSessionGuid = Guid.Empty,
                ErrorMessage = errorMessage
            });
        }
    }
}
