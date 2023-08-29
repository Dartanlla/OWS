using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace OWSParty.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartyController : ControllerBase
    {

        private readonly ILogger<PartyController> _logger;

        public PartyController(ILogger<PartyController> logger)
        {
            _logger = logger;
        }

        
    }
}