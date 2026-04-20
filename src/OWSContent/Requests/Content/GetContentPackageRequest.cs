using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace OWSContent.Requests.Content
{
    public class GetContentPackageRequest : IRequestHandler<GetContentPackageRequest, IActionResult>, IRequest
    {
        private readonly Guid _customerGuid;
        private readonly int _contentPackageId;
        private readonly IContentRepository _contentRepository;
        private readonly ILogger _logger;

        public GetContentPackageRequest(Guid customerGuid, int contentPackageId, IContentRepository contentRepository, ILogger logger)
        {
            _customerGuid = customerGuid;
            _contentPackageId = contentPackageId;
            _contentRepository = contentRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Handle()
        {
            var contentPackage = await _contentRepository.GetContentPackageAsync(_customerGuid, _contentPackageId);

            if (contentPackage == null)
                return new NotFoundObjectResult($"No ContentPackage found with id: {_contentPackageId}");

            return new OkObjectResult(contentPackage);
        }
    }
}
