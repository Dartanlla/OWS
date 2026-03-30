using Microsoft.AspNetCore.Mvc;
using OWSContent.DTOs;
using OWSContent.Requests.ContentCache;
using OWSData.Repositories.Interfaces;
using Serilog;
using System.Threading.Tasks;

namespace OWSContent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentCacheController : Controller
    {
        private readonly ILogger _logger;
        private readonly IContentCacheRepository _contentCacheRepository;

        public ContentCacheController(ILogger logger, IContentCacheRepository contentCacheRepository)
        {
            _logger = logger;
            _contentCacheRepository = contentCacheRepository;
        }

        [HttpPost]
        [Route("AddOrUpdateContentCache")]
        [RequestSizeLimit(200 * 1024 * 1024)]
        public async Task<IActionResult> AddOrUpdateContentCache([FromBody] AddOrUpdateContentCacheDTO addOrUpdateContentCacheDTO)
        {
            _logger.Information("AddOrUpdateContentCache Controller Called");
            AddOrUpdateContentCacheRequest request = new AddOrUpdateContentCacheRequest(addOrUpdateContentCacheDTO, _contentCacheRepository, _logger);
            return await request.Handle();
        }

        [HttpGet]
        [Route("GetContentCache")]
        public async Task<IActionResult> GetContentCache([FromQuery] string zoneName)
        {
            _logger.Information("GetContentCache Controller Called");
            GetContentCacheRequest request = new GetContentCacheRequest(zoneName, _contentCacheRepository, _logger);
            return await request.Handle();
        }
    }
}
