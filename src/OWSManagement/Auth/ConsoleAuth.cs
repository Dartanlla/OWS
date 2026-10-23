using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using SimpleInjector;

namespace OWSManagement.Auth
{
    /// <summary>
    /// Console sign-in: a Steam account whose Users.Role is Moderator or above gets a cookie session. Policies match the
    /// in-game staff command roles.
    /// </summary>
    public static class ConsoleAuth
    {
        public const string Moderator = UserRoles.Moderator;
        public const string GameMaster = UserRoles.GameMaster;
        public const string Admin = UserRoles.Admin;

        public const string CookieName = "samsara_console";
        public const string LoginStateCookie = "samsara_login_state";
        public const string CsrfHeader = "X-Samsara-Console";

        private const string SteamIdClaim = "steamid";
        private const string RoleCheckedClaim = "role_checked";
        private static readonly TimeSpan RoleRecheckInterval = TimeSpan.FromSeconds(60);

        public static void AddPolicies(AuthorizationOptions options)
        {
            foreach (string role in new[] { Moderator, GameMaster, Admin })
            {
                int rank = UserRoles.Rank(role);
                options.AddPolicy(role, policy => policy.RequireAuthenticatedUser().RequireAssertion(context => Rank(context.User) >= rank));
            }
        }

        public static int Rank(ClaimsPrincipal user)
        {
            return user?.Identity?.IsAuthenticated == true ? UserRoles.Rank(user.FindFirstValue(ClaimTypes.Role)) : 0;
        }

        public static bool HasRole(ClaimsPrincipal user, string role) => Rank(user) >= UserRoles.Rank(role);

        public static ClaimsPrincipal CreatePrincipal(ConsoleStaffUser user)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserGUID.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName ?? ""),
                new Claim(ClaimTypes.Role, UserRoles.NormalizeOrDefault(user.Role)),
                new Claim(SteamIdClaim, user.SteamId ?? ""),
                new Claim(RoleCheckedClaim, DateTime.UtcNow.Ticks.ToString())
            }, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        /// <summary>"Name (steam:7656...)", the actor on AuditLog rows and zone commands.</summary>
        public static string Actor(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true) return "console";
            return $"{user.FindFirstValue(ClaimTypes.Name)} (steam:{user.FindFirstValue(SteamIdClaim)})";
        }

        public static object Describe(ClaimsPrincipal user) => new
        {
            Name = user.FindFirstValue(ClaimTypes.Name),
            Role = user.FindFirstValue(ClaimTypes.Role),
            SteamId = user.FindFirstValue(SteamIdClaim)
        };

        /// <summary>Re-reads the role every 60 s, so a demoted or removed staff member loses the console within a minute.</summary>
        public static async Task ValidatePrincipal(CookieValidatePrincipalContext context, Container container)
        {
            ClaimsPrincipal user = context.Principal;
            if (long.TryParse(user?.FindFirstValue(RoleCheckedClaim), out long ticks)
                && DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) < RoleRecheckInterval)
            {
                return;
            }

            ConsoleStaffUser current = null;
            if (Guid.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userGUID))
            {
                current = await container.GetInstance<IConsoleQueriesRepository>().GetUser(container.GetInstance<ConsoleCustomer>().CustomerGUID, userGUID);
            }

            if (current == null || !UserRoles.IsStaff(current.Role))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            context.ReplacePrincipal(CreatePrincipal(current));
            context.ShouldRenew = true;
        }

        /// <summary>
        /// Every API write must carry X-Samsara-Console. A cross-site form cannot set a custom header, and the console
        /// allows no CORS, so this and the SameSite cookie together stop request forgery.
        /// </summary>
        public static async Task RequireCsrfHeader(HttpContext context, Func<Task> next)
        {
            HttpRequest request = context.Request;
            bool isWrite = !(HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method));
            if (isWrite && request.Path.StartsWithSegments("/api") && request.Headers[CsrfHeader] != "1")
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Missing " + CsrfHeader + " header.");
                return;
            }
            await next();
        }
    }

    /// <summary>
    /// The one customer the console manages: OWSManagementOptions:CustomerGUID, else the only Customers row. Resolved
    /// once, on first use.
    /// </summary>
    public class ConsoleCustomer
    {
        private readonly Lazy<Guid> _customerGUID;

        public ConsoleCustomer(string configured, IConsoleQueriesRepository queries)
        {
            _customerGUID = new Lazy<Guid>(() =>
            {
                if (Guid.TryParse(configured, out Guid fromConfig) && fromConfig != Guid.Empty) return fromConfig;

                List<Guid> customers = queries.GetCustomerGUIDs().GetAwaiter().GetResult().ToList();
                if (customers.Count != 1)
                {
                    throw new InvalidOperationException(
                        $"Set OWSManagementOptions__CustomerGUID: the database has {customers.Count} customers, so the console cannot pick one.");
                }
                return customers[0];
            });
        }

        public Guid CustomerGUID => _customerGUID.Value;
    }

    /// <summary>IHeaderCustomerGUID for the console's controllers and repositories: always the console's customer.</summary>
    public class ConsoleCustomerGUID : IHeaderCustomerGUID
    {
        private readonly ConsoleCustomer _customer;

        public ConsoleCustomerGUID(ConsoleCustomer customer)
        {
            _customer = customer;
        }

        public Guid CustomerGUID
        {
            get => _customer.CustomerGUID;
            set { }
        }
    }
}
