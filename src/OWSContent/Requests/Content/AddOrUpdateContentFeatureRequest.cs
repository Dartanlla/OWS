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
    public class AddOrUpdateContentFeatureRequest : IRequestHandler<AddOrUpdateContentFeatureRequest, IActionResult>, IRequest
    {
        private readonly AddOrUpdateContentFeatureDTO _dto;
        private readonly IContentRepository _contentRepository;
        private readonly ILogger _logger;

        public AddOrUpdateContentFeatureRequest(AddOrUpdateContentFeatureDTO dto, IContentRepository contentRepository, ILogger logger)
        {
            _dto = dto;
            _contentRepository = contentRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Handle()
        {
            if (_dto.CustomerGuid == Guid.Empty)
                return new BadRequestObjectResult("CustomerGuid is required.");

            if (_dto.ContentFeatureJson.ValueKind == JsonValueKind.Undefined)
                return new BadRequestObjectResult("ContentFeatureJson is required.");

            var contentFeature = new ContentFeature
            {
                ContentFeatureId = _dto.ContentFeatureId,
                CustomerGuid = _dto.CustomerGuid,
                ContentFeatureJson = JsonSerializer.Serialize(_dto.ContentFeatureJson),
                CreatedBy = _dto.CreatedBy,
                ContentPackageId = _dto.ContentPackageId
            };

            _logger.Information("AddOrUpdateContentFeature called for CustomerGuid {CustomerGuid}", _dto.CustomerGuid);
            int resultId = await _contentRepository.AddOrUpdateContentFeatureAsync(_dto.CustomerGuid, contentFeature);
            return new OkObjectResult(new { ContentFeatureId = resultId });
        }
    }
}
