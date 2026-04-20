using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace OWSContent.Requests.Content
{
    public class GetContentFeatureRequest : IRequestHandler<GetContentFeatureRequest, IActionResult>, IRequest
    {
        private readonly Guid _customerGuid;
        private readonly int _contentFeatureId;
        private readonly IContentRepository _contentRepository;
        private readonly ILogger _logger;

        public GetContentFeatureRequest(Guid customerGuid, int contentFeatureId, IContentRepository contentRepository, ILogger logger)
        {
            _customerGuid = customerGuid;
            _contentFeatureId = contentFeatureId;
            _contentRepository = contentRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Handle()
        {
            var contentFeature = await _contentRepository.GetContentFeatureAsync(_customerGuid, _contentFeatureId);

            if (contentFeature == null)
                return new NotFoundObjectResult($"No ContentFeature found with id: {_contentFeatureId}");

            return new OkObjectResult(contentFeature);
        }
    }
}
