using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSManagement.Auth;
using Serilog;

namespace OWSManagement.Controllers
{
    /// <summary>Sign in through Steam, sign out, and who is signed in.</summary>
    [ApiController]
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly IConsoleQueriesRepository _queries;
        private readonly IAuditRepository _audit;
        private readonly ConsoleCustomer _customer;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IConsoleQueriesRepository queries, IAuditRepository audit, ConsoleCustomer customer, IHttpClientFactory httpClientFactory)
        {
            _queries = queries;
            _audit = audit;
            _customer = customer;
            _httpClientFactory = httpClientFactory;
        }

        private string Origin => $"{Request.Scheme}://{Request.Host}";

        private string ReturnTo(string state) => $"{Origin}/auth/steam/callback?state={Uri.EscapeDataString(state)}";

        [HttpGet]
        [Route("auth/steam")]
        public IActionResult SignIn()
        {
            string state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            // Lax, not Strict: the browser must send it on Steam's redirect back.
            Response.Cookies.Append(ConsoleAuth.LoginStateCookie, state, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                MaxAge = TimeSpan.FromMinutes(10)
            });
            return Redirect(SteamOpenId.BuildLoginUrl(ReturnTo(state), Origin + "/"));
        }

        [HttpGet]
        [Route("auth/steam/callback")]
        public async Task<IActionResult> Callback([FromQuery] string state)
        {
            string expectedState = Request.Cookies[ConsoleAuth.LoginStateCookie];
            Response.Cookies.Delete(ConsoleAuth.LoginStateCookie);
            if (string.IsNullOrEmpty(state) || state != expectedState)
            {
                return Refuse("bad_state", null);
            }

            Dictionary<string, string> response = Request.Query.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
            string steamId = SteamOpenId.ReadSteamId(response, ReturnTo(state), DateTime.UtcNow, out string error);
            if (steamId == null)
            {
                return Refuse(error, null);
            }

            if (!await SteamOpenId.VerifyWithSteam(_httpClientFactory.CreateClient("SteamOpenId"), response))
            {
                return Refuse("steam_rejected", steamId);
            }

            ConsoleStaffUser user = await _queries.FindUserBySteamId(_customer.CustomerGUID, steamId);
            if (user == null || !UserRoles.IsStaff(user.Role))
            {
                await _audit.Add(_customer.CustomerGUID, "management", "steam:" + steamId, "console_login", "refused",
                    new { reason = user == null ? "no_account" : "not_staff" }, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Refuse("not_staff", steamId);
            }

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, ConsoleAuth.CreatePrincipal(user));
            await _audit.Add(_customer.CustomerGUID, "management", $"{user.FirstName} (steam:{steamId})", "console_login", "ok",
                new { role = user.Role }, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Redirect("/");
        }

        [HttpPost]
        [Route("api/auth/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        [HttpGet]
        [Route("api/auth/me")]
        public IActionResult Me()
        {
            if (User?.Identity?.IsAuthenticated != true) return Unauthorized();
            return Ok(ConsoleAuth.Describe(User));
        }

        private IActionResult Refuse(string reason, string steamId)
        {
            Log.Warning("console_login_refused {Reason} {SteamId}", reason, steamId);
            return Redirect("/?login_error=" + Uri.EscapeDataString(reason ?? "unknown"));
        }
    }
}
