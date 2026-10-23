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
    /// Live instances, broadcasts and the console's switches: message of the day, logins open, region status.
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
        private readonly ConsoleCustomer _customer;

        public WorldController(IInstanceManagementRepository instances, IConsoleSettingsRepository settings, IZoneOpsRepository zoneOps,
            IHttpClientFactory httpClientFactory, ConsoleCustomer customer)
        {
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
            return Ok(new
            {
                Instances = instances,
                Regions = instances.Select(i => i.ServerRegion).Where(r => !string.IsNullOrEmpty(r)).Distinct().OrderBy(r => r),
                Settings = await _settings.GetAllRows(),
                Commands = await _zoneOps.GetRecentCommands(_customer.CustomerGUID, "", 50)
            });
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
