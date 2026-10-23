using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.Options;

namespace OWSPublicAPI.Controllers
{
    /// <summary>
    /// Public game-server list API.
    /// </summary>
    /// <remarks>
    /// Serves the server-selector list shown on the login screen. Entries come from the
    /// GameServersConfig appsettings section; population is config-stubbed for now.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ServersController : Controller
    {
        private readonly IOptions<GameServersOptions> _gameServersOptions;
        private readonly IConsoleSettingsRepository _consoleSettings;

        /// <summary>
        /// Constructor for public game-server list API.
        /// </summary>
        public ServersController(IOptions<GameServersOptions> gameServersOptions, IConsoleSettingsRepository consoleSettings)
        {
            _gameServersOptions = gameServersOptions;
            _consoleSettings = consoleSettings;
        }

        /// <summary>
        /// Get the list of game servers for the server selector.
        /// </summary>
        [HttpGet]
        [Route("List")]
        public async Task<IActionResult> List()
        {
            //The staff console can set a region's status (region_status:<Region>) without a config change and restart.
            IReadOnlyDictionary<string, string> settings = await _consoleSettings.GetAll();

            //A blank Name is how an environment switches an entry off: config arrays merge by index, so an
            //env-supplied row cannot be removed, only emptied.
            var servers = _gameServersOptions.Value.Servers
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(s => new
                {
                    s.Name,
                    s.Region,
                    Status = settings.TryGetValue(ConsoleSettingKeys.RegionStatusPrefix + s.Region, out string status) ? status : s.Status,
                    s.Population,
                    s.PingHost
                });

            return new OkObjectResult(new { Servers = servers });
        }
    }
}
