using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSManagement.Auth;

namespace OWSManagement.Controllers
{
    /// <summary>
    /// Live instances, broadcasts, the live map and the console's switches: message of the day, logins open, region
    /// status, scheduled maintenance, rate events and the player cap.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WorldController : Controller
    {
        private static readonly TimeSpan BroadcastLifetime = TimeSpan.FromMinutes(2);
        private static readonly string[] RegionStatuses = { "online", "maintenance", "offline" };

        private readonly IInstanceManagementRepository _instances;
        private readonly IConsoleSettingsRepository _settings;
        private readonly IZoneOpsRepository _zoneOps;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoginQueueRepository _queue;
        private readonly ConsoleCustomer _customer;

        public WorldController(IInstanceManagementRepository instances, IConsoleSettingsRepository settings, IZoneOpsRepository zoneOps,
            IHttpClientFactory httpClientFactory, ILoginQueueRepository queue, ConsoleCustomer customer)
        {
            _queue = queue;
            _instances = instances;
            _settings = settings;
            _zoneOps = zoneOps;
            _httpClientFactory = httpClientFactory;
            _customer = customer;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Get()
        {
            IEnumerable<ZoneInstanceSummary> instances = await _instances.GetLiveZoneInstances(_customer.CustomerGUID);
            IReadOnlyDictionary<string, string> settings = await _settings.GetAll();
            return Ok(new
            {
                Instances = instances,
                Regions = instances.Select(i => i.ServerRegion).Where(r => !string.IsNullOrEmpty(r)).Distinct().OrderBy(r => r),
                Settings = await _settings.GetAllRows(),
                Commands = await _zoneOps.GetRecentCommands(_customer.CustomerGUID, "", 50),
                Maintenance = ConsoleSettingValues.Maintenance(settings),
                Rates = ConsoleSettingValues.Rates(settings),
                PlayerCap = ConsoleSettingValues.PlayerCap(settings),
                Waiting = await _queue.Waiting(_customer.CustomerGUID)
            });
        }

        [HttpGet]
        [Route("Positions")]
        public async Task<IEnumerable<LivePosition>> Positions([FromQuery] string map)
        {
            return await _zoneOps.GetPositions(_customer.CustomerGUID, map);
        }

        public class MaintenanceInput
        {
            public DateTime StartsAt { get; set; }
            public string Message { get; set; }
        }

        /// <summary>Zones warn players 15, 5 and 1 minutes before the start; from the start, players below Moderator are kept out.</summary>
        [HttpPut]
        [Route("Maintenance")]
        [Authorize(Policy = ConsoleAuth.Admin)]
        public async Task<IActionResult> SetMaintenance([FromBody] MaintenanceInput input)
        {
            string message = input?.Message?.Trim() ?? "";
            if (input == null || input.StartsAt == default || message.Length > 300)
            {
                return BadRequest(new SuccessAndErrorMessage { Success = false, ErrorMessage = "Give a start time and up to 300 characters." });
            }
            await _settings.Set(ConsoleSettingKeys.Maintenance, ConsoleSettingValues.Write(new MaintenanceWindow
            {
                StartsAt = input.StartsAt.ToUniversalTime(),
                Message = message
            }), ConsoleAuth.Actor(User));
            return Ok(new SuccessAndErrorMessage { Success = true, ErrorMessage = "" });
        }

        [HttpDelete]
        [Route("Maintenance")]
        [Authorize(Policy = ConsoleAuth.Admin)]
        public async Task<IActionResult> EndMaintenance()
        {
            await _settings.Set(ConsoleSettingKeys.Maintenance, "", ConsoleAuth.Actor(User));
            return Ok(new SuccessAndErrorMessage { Success = true, ErrorMessage = "" });
        }

        [HttpPut]
        [Route("RateEvent")]
        [Authorize(Policy = ConsoleAuth.Admin)]
        public async Task<IActionResult> SetRateEvent([FromBody] RateEvent input)
        {
            string name = input?.Name?.Trim() ?? "";
            static bool StepOk(double value) => value >= RateEvent.MinMultiplier && value <= RateEvent.MaxMultiplier
                && Math.Abs(value * 4 - Math.Round(value * 4)) < 1e-9;
            if (input == null || name.Length is 0 or > 60 || !StepOk(input.Exp) || !StepOk(input.Drop)
                || input.EndsAt <= input.StartsAt || input.EndsAt.ToUniversalTime() <= DateTime.UtcNow)
            {
                return BadRequest(new SuccessAndErrorMessage
                {
                    Success = false,
                    ErrorMessage = "Give a name, multipliers from 1 to 5 in steps of 0.25, and an end after the start and after now."
                });
            }
            await _settings.Set(ConsoleSettingKeys.RateEvent, ConsoleSettingValues.Write(new RateEvent
            {
                Name = name,
                Exp = input.Exp,
                Drop = input.Drop,
                StartsAt = input.StartsAt.ToUniversalTime(),
                EndsAt = input.EndsAt.ToUniversalTime()
            }), ConsoleAuth.Actor(User));
            return Ok(new SuccessAndErrorMessage { Success = true, ErrorMessage = "" });
        }

        [HttpDelete]
        [Route("RateEvent")]
        [Authorize(Policy = ConsoleAuth.Admin)]
        public async Task<IActionResult> EndRateEvent()
        {
            await _settings.Set(ConsoleSettingKeys.RateEvent, "", ConsoleAuth.Actor(User));
            return Ok(new SuccessAndErrorMessage { Success = true, ErrorMessage = "" });
        }

        public class PlayerCapInput
        {
            public int Cap { get; set; }
        }

        /// <summary>0 turns the cap off. The cap counts players in the world; staff are never held in line.</summary>
        [HttpPut]
        [Route("PlayerCap")]
        [Authorize(Policy = ConsoleAuth.Admin)]
        public async Task<IActionResult> SetPlayerCap([FromBody] PlayerCapInput input)
        {
            if (input == null || input.Cap < 0 || input.Cap > 10000)
            {
                return BadRequest(new SuccessAndErrorMessage { Success = false, ErrorMessage = "Give a cap from 0 (off) to 10000." });
            }
            await _settings.Set(ConsoleSettingKeys.PlayerCap, input.Cap == 0 ? "" : input.Cap.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ConsoleAuth.Actor(User));
            return Ok(new SuccessAndErrorMessage { Success = true, ErrorMessage = "" });
        }

        public class BroadcastInput
        {
            public string Text { get; set; }
        }

        [HttpPost]
        [Route("Broadcast")]
        [Authorize(Policy = ConsoleAuth.GameMaster)]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastInput input)
        {
            string text = input?.Text?.Trim();
            if (string.IsNullOrEmpty(text) || text.Length > 300)
            {
                return BadRequest(new SuccessAndErrorMessage { Success = false, ErrorMessage = "Write 1 to 300 characters." });
            }

            int zones = await _zoneOps.AddCommandForAllInstances(_customer.CustomerGUID, ZoneCommandNames.Announce,
                JsonSerializer.Serialize(new { text }), ConsoleAuth.Actor(User), BroadcastLifetime);
            return Ok(new { Success = true, Zones = zones });
        }

        public class SettingInput
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        [HttpPut]
        [Route("Settings")]
        [Authorize(Policy = ConsoleAuth.Admin)]
        public async Task<IActionResult> SetSetting([FromBody] SettingInput input)
        {
            string key = input?.Key?.Trim() ?? "";
            string value = input?.Value?.Trim() ?? "";

            bool valid = key switch
            {
                ConsoleSettingKeys.Motd => value.Length <= 500,
                ConsoleSettingKeys.LoginsOpen => value is "" or "true" or "false",
                _ => key.StartsWith(ConsoleSettingKeys.RegionStatusPrefix) && key.Length > ConsoleSettingKeys.RegionStatusPrefix.Length
                     && (value == "" || RegionStatuses.Contains(value))
            };
            if (!valid)
            {
                return BadRequest(new SuccessAndErrorMessage { Success = false, ErrorMessage = "Unknown setting or value." });
            }

            await _settings.Set(key, value, ConsoleAuth.Actor(User));
            return Ok(new SuccessAndErrorMessage { Success = true, ErrorMessage = "" });
        }

        /// <summary>Asks InstanceManagement to stop the instance, as an idle shutdown would; its players are disconnected.</summary>
        [HttpPost]
        [Route("Instances/{mapInstanceId:int}/Stop")]
        [Authorize(Policy = ConsoleAuth.Admin)]
        public async Task<IActionResult> StopInstance(int mapInstanceId)
        {
            ZoneInstanceSummary instance = (await _instances.GetLiveZoneInstances(_customer.CustomerGUID)).FirstOrDefault(i => i.MapInstanceID == mapInstanceId);
            if (instance == null) return NotFound();

            HttpClient client = _httpClientFactory.CreateClient("OWSInstanceManagement");
            using var shutdown = new HttpRequestMessage(HttpMethod.Post, "api/Instance/ShutDownServerInstance")
            {
                Content = JsonContent.Create(new { WorldServerID = instance.WorldServerID, ZoneInstanceID = mapInstanceId })
            };
            shutdown.Headers.Add("X-CustomerGUID", _customer.CustomerGUID.ToString());
            using HttpResponseMessage response = await client.SendAsync(shutdown);
            SuccessAndErrorMessage result = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<SuccessAndErrorMessage>()
                : new SuccessAndErrorMessage { Success = false, ErrorMessage = "InstanceManagement returned " + (int)response.StatusCode };
            return Ok(result);
        }
    }
}
