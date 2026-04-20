using Microsoft.AspNetCore.Mvc;
using OWSContent.DTOs;
using OWSData.Models.Content;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using Serilog;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace OWSContent.Requests.Content
{
    public class AddOrUpdateContentPackageRequest : IRequestHandler<AddOrUpdateContentPackageRequest, IActionResult>, IRequest
    {
        private readonly AddOrUpdateContentPackageDTO _dto;
        private readonly IContentRepository _contentRepository;
        private readonly ILogger _logger;

        public AddOrUpdateContentPackageRequest(AddOrUpdateContentPackageDTO dto, IContentRepository contentRepository, ILogger logger)
        {
            _dto = dto;
            _contentRepository = contentRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Handle()
        {
            if (_dto.CustomerGuid == Guid.Empty)
                return new BadRequestObjectResult("CustomerGuid is required.");

            if (_dto.AggregatedContentJson.ValueKind == JsonValueKind.Undefined)
                return new BadRequestObjectResult("AggregatedContentJson is required.");

            var contentPackage = new ContentPackage
            {
                ContentPackageId = _dto.ContentPackageId,
                CustomerGuid = _dto.CustomerGuid,
                AggregatedContentJson = JsonSerializer.Serialize(_dto.AggregatedContentJson),
                VersionNumber = _dto.VersionNumber,
                DeployAt = _dto.DeployAt
            };

            _logger.Information("AddOrUpdateContentPackage called for CustomerGuid {CustomerGuid}", _dto.CustomerGuid);
            int resultId = await _contentRepository.AddOrUpdateContentPackageAsync(_dto.CustomerGuid, contentPackage);
            return new OkObjectResult(new { ContentPackageId = resultId });
        }
    }
}
