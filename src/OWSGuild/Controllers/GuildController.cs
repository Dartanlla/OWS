using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace OWSGuild.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GuildController : ControllerBase
    {

        private readonly ILogger<GuildController> _logger;

        public GuildController(ILogger<GuildController> logger)
        {
            _logger = logger;
        }

        
    }
}