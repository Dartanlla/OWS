using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSShared.Options;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSManagement.Auth;

namespace OWSManagement.Controllers
{
    /// <summary>
    /// Accounts and characters: search, the account and character pages, sanctions, and commands for a character's
    /// zone server. Reads need Moderator; each action checks its own role.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PlayersController : Controller
    {
        // A command for an online character expires fast: if they log off first, it is moot. Gifts, unstuck and
        // warnings wait for the next login.
        private static readonly TimeSpan OnlineOnlyLifetime = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan NextLoginLifetime = TimeSpan.FromDays(30);
        private const int MaxJuriGift = 1000000000;

        private readonly IConsoleQueriesRepository _queries;
        private readonly ISanctionsRepository _sanctions;
        private readonly IZoneOpsRepository _zoneOps;
        private readonly IChatLogRepository _chatLog;
        private readonly IUsersRepository _users;
        private readonly ICharactersRepository _characters;
        private readonly IPlayerReportsRepository _reports;
        private readonly IConsoleInsightRepository _insight;
        private readonly string _sentryUrl;
        private readonly ConsoleCustomer _customer;

        public PlayersController(IConsoleQueriesRepository queries, ISanctionsRepository sanctions, IZoneOpsRepository zoneOps,
            IChatLogRepository chatLog, IUsersRepository users, ICharactersRepository characters, IPlayerReportsRepository reports,
            IConsoleInsightRepository insight, IOptions<ManagementOptions> options, ConsoleCustomer customer)
        {
            _sentryUrl = options.Value.SentryUrl;
            _reports = reports;
            _insight = insight;
            _characters = characters;
            _queries = queries;
            _sanctions = sanctions;
            _zoneOps = zoneOps;
            _chatLog = chatLog;
            _users = users;
            _customer = customer;
        }

        private Guid CustomerGUID => _customer.CustomerGUID;

        [HttpGet]
        [Route("Search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            return Ok(new
            {
                Accounts = await _queries.SearchAccounts(CustomerGUID, q, 50),
                Characters = await _queries.SearchCharacters(CustomerGUID, q, 100)
            });
        }

        [HttpGet]
        [Route("Distribution")]
        public async Task<ConsoleDistribution> Distribution()
        {
            return await _queries.GetDistribution(CustomerGUID);
        }

        [HttpGet]
        [Route("Account/{userGuid:guid}")]
        public async Task<IActionResult> Account(Guid userGuid)
        {
            ConsoleAccountRow account = await _queries.GetAccount(CustomerGUID, userGuid);
            if (account == null) return NotFound();

            return Ok(new
            {
                Account = account,
                Characters = await _queries.GetCharactersForAccount(CustomerGUID, userGuid),
                Logins = await _queries.GetLoginsForAccount(CustomerGUID, userGuid, 50),
                Sanctions = await _sanctions.GetForUser(CustomerGUID, userGuid)
            });
        }

        [HttpGet]
        [Route("Character/{name}")]
        public async Task<IActionResult> Character(string name)
        {
            ConsoleCharacterDetail character = await _queries.GetCharacter(CustomerGUID, name);
            if (character == null) return NotFound();

            return Ok(new
            {
                Character = character,
                Commands = await _zoneOps.GetRecentCommands(CustomerGUID, character.CharName, 50),
                Audit = await _queries.GetAuditForCharacter(CustomerGUID, character.CharName, 100),
                Reports = await _reports.ForCharacter(CustomerGUID, character.CharName, 50),
                Trades = await _insight.GetTradePartners(CustomerGUID, character.CharName),
                SentrySearch = SentrySearch(character.CharName)
            });
        }

        /// <summary>
        /// Sentry's issue search for the character tag (bug reports and crashes), from the configured Sentry link: an
        /// org subdomain (https://org.sentry.io/...) or an /organizations/org/ path. Null when no link is configured.
        /// </summary>
        private string SentrySearch(string characterName)
        {
            if (!Uri.TryCreate(_sentryUrl, UriKind.Absolute, out Uri sentry)) return null;
            string query = "?query=" + Uri.EscapeDataString($"character:\"{characterName}\"") + "&statsPeriod=90d";
            System.Text.RegularExpressions.Match org = System.Text.RegularExpressions.Regex.Match(sentry.AbsolutePath, "/organizations/([^/]+)");
            return org.Success
                ? $"{sentry.GetLeftPart(UriPartial.Authority)}/organizations/{org.Groups[1].Value}/issues/{query}"
                : $"{sentry.GetLeftPart(UriPartial.Authority)}/issues/{query}";
        }

        [HttpGet]
        [Route("Character/{name}/Events")]
        public async Task<IEnumerable<GameEventRow>> Events(string name, [FromQuery] string @event, [FromQuery] long before = 0, [FromQuery] int limit = 100)
        {
            return await _zoneOps.GetEvents(CustomerGUID, name, @event, before, limit);
        }

        [HttpGet]
        [Route("Character/{name}/Chat")]
        public async Task<IEnumerable<ChatLogEntry>> Chat(string name, [FromQuery] long before = 0)
        {
            return await _chatLog.Search(CustomerGUID, name, null, before, 100);
        }

        public class SanctionInput
        {
            public string Kind { get; set; }
            public string Reason { get; set; }

            /// <summary>Ban and mute only; 0 is permanent.</summary>
            public int Minutes { get; set; }

            /// <summary>The character it was issued against, for context. Optional.</summary>
            public string CharacterName { get; set; }
        }

        [HttpPost]
        [Route("Account/{userGuid:guid}/Sanctions")]
        public async Task<IActionResult> AddSanction(Guid userGuid, [FromBody] SanctionInput input)
        {
            string kind = input?.Kind?.Trim().ToLowerInvariant();
            if (!SanctionKinds.All.Contains(kind)) return BadRequest(Refused("Unknown sanction kind."));
            if (kind == SanctionKinds.Ban && !ConsoleAuth.HasRole(User, ConsoleAuth.Admin)) return Forbid();
            if (string.IsNullOrWhiteSpace(input.Reason)) return BadRequest(Refused("A reason is required."));
            if (input.Minutes < 0) return BadRequest(Refused("Minutes cannot be negative."));

            ConsoleAccountRow account = await _queries.GetAccount(CustomerGUID, userGuid);
            if (account == null) return NotFound();
            if (UserRoles.Rank(account.Role) >= ConsoleAuth.Rank(User)) return BadRequest(Refused("You can only sanction accounts below your own role."));

            string actor = ConsoleAuth.Actor(User);
            DateTime? expiresAt = SanctionKinds.HasDuration(kind) && input.Minutes > 0 ? DateTime.UtcNow.AddMinutes(input.Minutes) : null;
            long sanctionId = await _sanctions.Add(CustomerGUID, userGuid, kind, input.Reason, input.CharacterName, expiresAt, actor, "console");

            Sanction issued = new Sanction { Kind = kind, Reason = input.Reason, ExpiresAt = expiresAt };
            List<string> online = (await _queries.GetOnlineCharacterNames(CustomerGUID, userGuid)).ToList();
            int queued = 0;
            foreach (string character in online)
            {
                switch (kind)
                {
                    case SanctionKinds.Ban:
                        await _zoneOps.AddCommand(CustomerGUID, null, character, ZoneCommandNames.Kick,
                            Args(new { reason = input.Reason.Trim(), message = issued.PlayerMessage() }), actor, OnlineOnlyLifetime);
                        queued++;
                        break;
                    case SanctionKinds.Mute:
                        await _zoneOps.AddCommand(CustomerGUID, null, character, ZoneCommandNames.Mute,
                            Args(new { minutes = input.Minutes, reason = input.Reason.Trim() }), actor, OnlineOnlyLifetime);
                        queued++;
                        break;
                    case SanctionKinds.Warn:
                        await _zoneOps.AddCommand(CustomerGUID, null, character, ZoneCommandNames.Warn, Args(new { reason = input.Reason.Trim() }), actor, OnlineOnlyLifetime);
                        queued++;
                        break;
                }
            }

            // An offline player sees a warning at their next login, on the character it was issued against.
            if (kind == SanctionKinds.Warn && online.Count == 0 && !string.IsNullOrWhiteSpace(input.CharacterName))
            {
                await _zoneOps.AddCommand(CustomerGUID, null, input.CharacterName, ZoneCommandNames.Warn, Args(new { reason = input.Reason.Trim() }), actor, NextLoginLifetime);
                queued++;
            }

            return Ok(new { Success = true, SanctionId = sanctionId, CommandsQueued = queued });
        }

        [HttpPost]
        [Route("Account/{userGuid:guid}/Sanctions/{sanctionId:long}/Revoke")]
        public async Task<IActionResult> RevokeSanction(Guid userGuid, long sanctionId)
        {
            Sanction sanction = await _sanctions.GetById(CustomerGUID, userGuid, sanctionId);
            if (sanction == null) return NotFound();
            if (sanction.Kind == SanctionKinds.Ban && !ConsoleAuth.HasRole(User, ConsoleAuth.Admin)) return Forbid();

            string actor = ConsoleAuth.Actor(User);
            if (!await _sanctions.Revoke(CustomerGUID, sanctionId, actor))
            {
                return Ok(Refused("Already revoked."));
            }

            if (sanction.Kind == SanctionKinds.Mute)
            {
                foreach (string character in await _queries.GetOnlineCharacterNames(CustomerGUID, userGuid))
                {
                    await _zoneOps.AddCommand(CustomerGUID, null, character, ZoneCommandNames.Unmute, "{}", actor, OnlineOnlyLifetime);
                }
            }
            return Ok(new { Success = true });
        }

        public class RoleInput
        {
            public string Role { get; set; }
        }

        [HttpPut]
        [Route("Account/{userGuid:guid}/Role")]
        public async Task<IActionResult> SetRole(Guid userGuid, [FromBody] RoleInput input)
        {
            if (!ConsoleAuth.HasRole(User, ConsoleAuth.Admin)) return Forbid();
            if (!UserRoles.TryNormalize(input?.Role, out string role)) return BadRequest(Refused("Unknown role."));
            if (User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == userGuid.ToString())
            {
                return BadRequest(Refused("You cannot change your own role."));
            }

            return Ok(await _users.UpdateUserRole(CustomerGUID, userGuid, role));
        }

        public class CommandInput
        {
            public string Command { get; set; }
            public string Reason { get; set; }
            public string ItemTag { get; set; }
            public int Count { get; set; }
            public int Amount { get; set; }
        }

        /// <summary>
        /// Queues a command for the character's zone server. Kick needs Moderator and runs only while the character is
        /// online; unstuck and gifts need GameMaster and wait up to 30 days for the next login.
        /// </summary>
        [HttpPost]
        [Route("Character/{name}/Commands")]
        public async Task<IActionResult> AddCommand(string name, [FromBody] CommandInput input)
        {
            ConsoleCharacterDetail character = await _queries.GetCharacter(CustomerGUID, name);
            if (character == null) return NotFound();

            string actor = ConsoleAuth.Actor(User);
            string reason = input?.Reason?.Trim() ?? "";
            string command = input?.Command?.Trim().ToLowerInvariant();
            (string args, TimeSpan lifetime, string role) = command switch
            {
                ZoneCommandNames.Kick => (Args(new { reason }), OnlineOnlyLifetime, ConsoleAuth.Moderator),
                ZoneCommandNames.Unstuck => ("{}", NextLoginLifetime, ConsoleAuth.GameMaster),
                ZoneCommandNames.GiveItem => (Args(new { itemTag = input.ItemTag?.Trim(), count = input.Count, reason }), NextLoginLifetime, ConsoleAuth.GameMaster),
                ZoneCommandNames.GiveJuri => (Args(new { amount = input.Amount, reason }), NextLoginLifetime, ConsoleAuth.GameMaster),
                _ => (null, TimeSpan.Zero, null)
            };

            if (role == null) return BadRequest(Refused("Unknown command."));
            if (!ConsoleAuth.HasRole(User, role)) return Forbid();
            if (command == ZoneCommandNames.GiveItem && (string.IsNullOrEmpty(input.ItemTag) || !input.ItemTag.Trim().StartsWith("Item.") || input.Count < 1 || input.Count > 999))
            {
                return BadRequest(Refused("Give an item tag that starts with Item. and a count from 1 to 999."));
            }
            if (command == ZoneCommandNames.GiveJuri && (input.Amount < 1 || input.Amount > MaxJuriGift))
            {
                return BadRequest(Refused($"Give an amount from 1 to {MaxJuriGift:N0}."));
            }
            if (command != ZoneCommandNames.Kick && string.IsNullOrEmpty(reason))
            {
                return BadRequest(Refused("A reason is required."));
            }
            if (command == ZoneCommandNames.Kick && !character.OnlineMapInstanceID.HasValue)
            {
                return BadRequest(Refused("This character is not online."));
            }

            long commandId = await _zoneOps.AddCommand(CustomerGUID, null, character.CharName, command, args, actor, lifetime);
            return Ok(new { Success = true, CommandId = commandId });
        }

        public class FlagsInput
        {
            public bool? IsAdmin { get; set; }
            public bool? IsModerator { get; set; }
            public bool? IsInternalNetworkTestUser { get; set; }
        }

        [HttpPut]
        [Route("Character/{name}/Flags")]
        public async Task<IActionResult> SetFlags(string name, [FromBody] FlagsInput input)
        {
            if (!ConsoleAuth.HasRole(User, ConsoleAuth.Admin)) return Forbid();
            ConsoleCharacterDetail character = await _queries.GetCharacter(CustomerGUID, name);
            if (character == null) return NotFound();

            return Ok(await _characters.SetCharacterFlags(CustomerGUID, character.CharacterID, input?.IsAdmin, input?.IsModerator, input?.IsInternalNetworkTestUser));
        }

        private static string Args(object value) => JsonSerializer.Serialize(value);

        private static SuccessAndErrorMessage Refused(string message) => new SuccessAndErrorMessage { Success = false, ErrorMessage = message };
    }
}
