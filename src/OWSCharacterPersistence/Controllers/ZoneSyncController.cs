using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Implementations.Postgres;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using Serilog;

namespace OWSCharacterPersistence.Controllers
{
    /// <summary>
    /// A zone server's 5 s check-in with the staff console: it sends its queued game events, the results of the
    /// commands it ran and its players' positions, and gets its pending commands, the message of the day, the rate
    /// multipliers in force and any scheduled maintenance. Service key only.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ZoneSyncController : Controller
    {
        private const int MaxEventsPerCall = 500;

        private readonly IZoneOpsRepository _zoneOps;
        private readonly IConsoleSettingsRepository _settings;
        private readonly IHeaderCustomerGUID _customerGuid;

        public ZoneSyncController(IZoneOpsRepository zoneOps, IConsoleSettingsRepository settings, IHeaderCustomerGUID customerGuid)
        {
            _zoneOps = zoneOps;
            _settings = settings;
            _customerGuid = customerGuid;
        }

        public class ZoneSyncRequest
        {
            public int ZoneInstanceId { get; set; }
            public List<JsonElement> Events { get; set; }
            public List<ZoneCommandResult> Results { get; set; }
            public string MapName { get; set; }
            /// <summary>Null from a zone that does not send positions; empty when no player is on the zone.</summary>
            public List<LivePositionIn> Positions { get; set; }
        }

        public class ZoneSyncCommand
        {
            public long CommandId { get; set; }
            public int Attempt { get; set; }
            public string Command { get; set; }
            public string CharacterName { get; set; }
            public string CreatedBy { get; set; }
            public JsonElement Args { get; set; }
        }

        public class ZoneSyncResponse
        {
            public List<ZoneSyncCommand> Commands { get; set; } = new();
            public string Motd { get; set; } = "";
            public ZoneSyncRates Rates { get; set; } = new();
            public ZoneSyncMaintenance Maintenance { get; set; }
        }

        public class ZoneSyncRates
        {
            public double Exp { get; set; } = 1;
            public double Drop { get; set; } = 1;
            public string Name { get; set; } = "";
            /// <summary>Seconds until the event ends, so a zone that loses the backend keeps it until then. 0 with no event.</summary>
            public int EndsInSeconds { get; set; }
        }

        public class ZoneSyncMaintenance
        {
            /// <summary>Negative once maintenance has started. Relative, so zone clocks do not matter.</summary>
            public int StartsInSeconds { get; set; }
            public string Message { get; set; }
        }

        [HttpPost]
        [Route("")]
        public async Task<IActionResult> Sync([FromBody] ZoneSyncRequest request)
        {
            if (!SamsaraServiceKey.IsValid(Request)) return Unauthorized();
            if (request == null || request.ZoneInstanceId <= 0) return BadRequest();

            List<GameEventIn> events = (request.Events ?? new List<JsonElement>())
                .Take(MaxEventsPerCall)
                .Select(GameEventIn.FromJson)
                .Where(e => e != null)
                .ToList();
            if (events.Count > 0)
            {
                await _zoneOps.AddEvents(_customerGuid.CustomerGUID, request.ZoneInstanceId, events);
            }

            if (request.Results?.Any(r => r != null && r.Result == ZoneOpsRepository.RetryResult && r.Attempt <= 0) == true)
            {
                Log.Warning("zone_retry_without_attempt {ZoneInstanceId}", request.ZoneInstanceId);
            }
            await _zoneOps.CompleteCommands(_customerGuid.CustomerGUID, request.ZoneInstanceId, request.Results);

            if (request.Positions != null)
            {
                await _zoneOps.SetPositions(_customerGuid.CustomerGUID, request.ZoneInstanceId, request.MapName, request.Positions);
            }

            var response = new ZoneSyncResponse();
            foreach (ZoneCommand command in await _zoneOps.TakePendingCommands(_customerGuid.CustomerGUID, request.ZoneInstanceId))
            {
                response.Commands.Add(new ZoneSyncCommand
                {
                    CommandId = command.CommandID,
                    Attempt = command.DeliveryAttempt,
                    Command = command.Command,
                    CharacterName = command.CharacterName ?? "",
                    CreatedBy = command.CreatedBy,
                    Args = ParseArgs(command.Args)
                });
            }

            IReadOnlyDictionary<string, string> settings = await _settings.GetAll();
            response.Motd = settings.TryGetValue(ConsoleSettingKeys.Motd, out string motd) ? motd : "";

            DateTime now = DateTime.UtcNow;
            RateEvent rates = ConsoleSettingValues.Rates(settings);
            if (rates != null && rates.InForce(now))
            {
                response.Rates = new ZoneSyncRates
                {
                    Exp = rates.Exp,
                    Drop = rates.Drop,
                    Name = rates.Name ?? "",
                    EndsInSeconds = (int)Math.Ceiling((rates.EndsAt - now).TotalSeconds)
                };
            }
            MaintenanceWindow maintenance = ConsoleSettingValues.Maintenance(settings);
            if (maintenance != null)
            {
                response.Maintenance = new ZoneSyncMaintenance
                {
                    StartsInSeconds = (int)Math.Floor((maintenance.StartsAt - now).TotalSeconds),
                    Message = maintenance.PlayerMessage()
                };
            }

            if (response.Commands.Count > 0)
            {
                Log.Information("zone_commands_delivered {ZoneInstanceId} {CommandIds}", request.ZoneInstanceId,
                    string.Join(",", response.Commands.Select(c => c.CommandId)));
            }
            return Ok(response);
        }

        private static JsonElement ParseArgs(string args)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(string.IsNullOrWhiteSpace(args) ? "{}" : args);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                using JsonDocument empty = JsonDocument.Parse("{}");
                return empty.RootElement.Clone();
            }
        }
    }
}
