using Microsoft.AspNetCore.Mvc;
using OWSContent.DTOs;
using OWSShared.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSContent.Requests.Example
{
    public class PostRequest : IRequestHandler<PostRequest, IActionResult>, IRequest
    {

        private readonly ExampleDTO _exampleDTO;
        private ILogger _logger;
        private Guid _customerGUID;

        public PostRequest(ExampleDTO exampleDTO, ILogger logger, IHeaderCustomerGUID customerGuid)
        {
            _exampleDTO = exampleDTO;
            _logger = logger;
            _customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            _logger.Information($"Example Post Request {_exampleDTO.Message}");
            return new OkObjectResult($"Example Post Request {_exampleDTO.Message}");
        }
    }
}
