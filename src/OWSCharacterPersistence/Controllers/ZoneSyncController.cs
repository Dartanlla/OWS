using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using Serilog;

namespace OWSCharacterPersistence.Controllers
{
    /// <summary>
    /// A zone server's 5 s check-in with the staff console: it sends its queued game events and the results of the
    /// commands it ran, and gets its pending commands and the message of the day. Service key only.
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
        }

        public class ZoneSyncCommand
        {
            public long CommandId { get; set; }
            public string Command { get; set; }
            public string CharacterName { get; set; }
            public string CreatedBy { get; set; }
            public JsonElement Args { get; set; }
        }

        public class ZoneSyncResponse
        {
            public List<ZoneSyncCommand> Commands { get; set; } = new();
            public string Motd { get; set; } = "";
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

            await _zoneOps.CompleteCommands(_customerGuid.CustomerGUID, request.Results);

            var response = new ZoneSyncResponse();
            foreach (ZoneCommand command in await _zoneOps.TakePendingCommands(_customerGuid.CustomerGUID, request.ZoneInstanceId))
            {
                response.Commands.Add(new ZoneSyncCommand
                {
                    CommandId = command.CommandID,
                    Command = command.Command,
                    CharacterName = command.CharacterName ?? "",
                    CreatedBy = command.CreatedBy,
                    Args = ParseArgs(command.Args)
                });
            }

            IReadOnlyDictionary<string, string> settings = await _settings.GetAll();
            response.Motd = settings.TryGetValue(ConsoleSettingKeys.Motd, out string motd) ? motd : "";

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
