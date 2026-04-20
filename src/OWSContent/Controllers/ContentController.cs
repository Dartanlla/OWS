using Microsoft.AspNetCore.Mvc;
using OWSContent.DTOs;
using OWSContent.Requests.Content;
using OWSData.Repositories.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace OWSContent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentController : Controller
    {
        private readonly ILogger _logger;
        private readonly IContentRepository _contentRepository;

        public ContentController(ILogger logger, IContentRepository contentRepository)
        {
            _logger = logger;
            _contentRepository = contentRepository;
        }

        [HttpGet]
        [Route("GetContentFeature")]
        public async Task<IActionResult> GetContentFeature([FromQuery] Guid customerGuid, [FromQuery] int contentFeatureId)
        {
            _logger.Information("GetContentFeature Controller Called");
            GetContentFeatureRequest request = new GetContentFeatureRequest(customerGuid, contentFeatureId, _contentRepository, _logger);
            return await request.Handle();
        }

        [HttpPost]
        [Route("AddOrUpdateContentFeature")]
        public async Task<IActionResult> AddOrUpdateContentFeature([FromBody] AddOrUpdateContentFeatureDTO addOrUpdateContentFeatureDTO)
        {
            _logger.Information("AddOrUpdateContentFeature Controller Called");
            AddOrUpdateContentFeatureRequest request = new AddOrUpdateContentFeatureRequest(addOrUpdateContentFeatureDTO, _contentRepository, _logger);
            return await request.Handle();
        }

        [HttpGet]
        [Route("GetContentPackage")]
        public async Task<IActionResult> GetContentPackage([FromQuery] Guid customerGuid, [FromQuery] int contentPackageId)
        {
            _logger.Information("GetContentPackage Controller Called");
            GetContentPackageRequest request = new GetContentPackageRequest(customerGuid, contentPackageId, _contentRepository, _logger);
            return await request.Handle();
        }

        [HttpPost]
        [Route("AddOrUpdateContentPackage")]
        public async Task<IActionResult> AddOrUpdateContentPackage([FromBody] AddOrUpdateContentPackageDTO addOrUpdateContentPackageDTO)
        {
            _logger.Information("AddOrUpdateContentPackage Controller Called");
            AddOrUpdateContentPackageRequest request = new AddOrUpdateContentPackageRequest(addOrUpdateContentPackageDTO, _contentRepository, _logger);
            return await request.Handle();
        }
    }
}
