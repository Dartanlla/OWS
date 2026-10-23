using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSManagement.Auth;

namespace OWSManagement.Controllers
{
    /// <summary>The landing page: who is online, zone load, today's numbers and the last 24 h of signals.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OverviewController : Controller
    {
        private readonly IConsoleQueriesRepository _queries;
        private readonly IInstanceManagementRepository _instances;
        private readonly IConsoleSettingsRepository _settings;
        private readonly IPlayerReportsRepository _reports;
        private readonly ILoginQueueRepository _queue;
        private readonly ConsoleCustomer _customer;

        public OverviewController(IConsoleQueriesRepository queries, IInstanceManagementRepository instances, IConsoleSettingsRepository settings,
            IPlayerReportsRepository reports, ILoginQueueRepository queue, ConsoleCustomer customer)
        {
            _reports = reports;
            _queue = queue;
            _queries = queries;
            _instances = instances;
            _settings = settings;
            _customer = customer;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Get()
        {
            IReadOnlyDictionary<string, string> settings = await _settings.GetAll();
            return Ok(new
            {
                Overview = await _queries.GetOverview(_customer.CustomerGUID),
                Instances = await _instances.GetLiveZoneInstances(_customer.CustomerGUID),
                LoginsOpen = !(settings.TryGetValue(ConsoleSettingKeys.LoginsOpen, out string open) && open == "false"),
                Motd = settings.TryGetValue(ConsoleSettingKeys.Motd, out string motd) ? motd : "",
                Maintenance = ConsoleSettingValues.Maintenance(settings),
                Rates = ConsoleSettingValues.Rates(settings) is RateEvent rates && rates.EndsAt > DateTime.UtcNow ? rates : null,
                PlayerCap = ConsoleSettingValues.PlayerCap(settings),
                Waiting = await _queue.Waiting(_customer.CustomerGUID),
                OpenReports = await _reports.CountOpen(_customer.CustomerGUID)
            });
        }
    }
}
