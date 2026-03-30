using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using Serilog;
using System.Threading.Tasks;

namespace OWSContent.Requests.ContentCache
{
    public class GetContentCacheRequest : IRequestHandler<GetContentCacheRequest, IActionResult>, IRequest
    {
        private readonly string _zoneName;
        private readonly IContentCacheRepository _contentCacheRepository;
        private readonly ILogger _logger;

        public GetContentCacheRequest(string zoneName, IContentCacheRepository contentCacheRepository, ILogger logger)
        {
            _zoneName = zoneName;
            _contentCacheRepository = contentCacheRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Handle()
        {
            if (string.IsNullOrEmpty(_zoneName))
                return new BadRequestObjectResult("zoneName is required.");

            _logger.Information("GetContentCache called for zone {ZoneName}", _zoneName);
            var contentJson = await _contentCacheRepository.GetContentCacheAsync(_zoneName);

            if (contentJson == null)
                return new NotFoundObjectResult($"No content cache found for zone: {_zoneName}");

            return new ContentResult
            {
                Content = contentJson,
                ContentType = "application/json",
                StatusCode = 200
            };
        }
    }
}
