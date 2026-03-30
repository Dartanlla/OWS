using Microsoft.AspNetCore.Mvc;
using OWSContent.DTOs;
using OWSShared.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSContent.Requests.Example
{
    public class GetRequest : IRequestHandler<GetRequest, IActionResult>, IRequest
    {
        private ILogger _logger;
        private Guid _customerGUID;

        public GetRequest(ILogger logger, IHeaderCustomerGUID customerGuid)
        {
            _logger = logger;
            _customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            _logger.Information("Example Get Request");
            return new OkObjectResult("Example Get Request");
        }
    }
}
