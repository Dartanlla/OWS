using Microsoft.AspNetCore.Mvc;
using OWSContent.DTOs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using Serilog;
using System.Threading.Tasks;

namespace OWSContent.Requests.ContentCache
{
    public class AddOrUpdateContentCacheRequest : IRequestHandler<AddOrUpdateContentCacheRequest, IActionResult>, IRequest
    {
        private readonly AddOrUpdateContentCacheDTO _dto;
        private readonly IContentCacheRepository _contentCacheRepository;
        private readonly ILogger _logger;

        public AddOrUpdateContentCacheRequest(AddOrUpdateContentCacheDTO dto, IContentCacheRepository contentCacheRepository, ILogger logger)
        {
            _dto = dto;
            _contentCacheRepository = contentCacheRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Handle()
        {
            if (string.IsNullOrEmpty(_dto.ZoneName))
                return new BadRequestObjectResult("ZoneName is required.");

            if (string.IsNullOrEmpty(_dto.ContentJson))
                return new BadRequestObjectResult("ContentJson is required.");

            _logger.Information("AddOrUpdateContentCache called for zone {ZoneName}", _dto.ZoneName);
            await _contentCacheRepository.AddOrUpdateContentCacheAsync(_dto.ZoneName, _dto.ContentJson);
            return new OkObjectResult($"Content cache updated for zone: {_dto.ZoneName}");
        }
    }
}
