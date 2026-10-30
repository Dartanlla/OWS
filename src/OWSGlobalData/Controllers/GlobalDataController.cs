using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using SimpleInjector;
using OWSData.Models.StoredProcs;
using OWSShared.Interfaces;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared;
using OWSShared.Options;
using System.Net.Http;
using Serilog;
using OWSGlobalData.DTOs;
using OWSGlobalData.Requests;
using OWSData.Models.Tables;

namespace OWSGlobalData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GlobalDataController : Controller
    {
        private readonly IGlobalDataRepository _globalDataRepository;
        private readonly IHeaderCustomerGUID _customerGuid;

        public GlobalDataController(IGlobalDataRepository globalDataRepository, IHeaderCustomerGUID customerGuid)
        {
            _globalDataRepository = globalDataRepository;
            _customerGuid = customerGuid;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_customerGuid.CustomerGUID == Guid.Empty)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        /// <summary>
        /// Add an item to Global Data
        /// </summary>
        /// <remarks>
        /// Adds or updates a key/value pair to the Global Data system. Zone servers only: the CustomerGUID ships with
        /// game clients, so writes also need the X-Samsara-Service-Key. Reads stay on the CustomerGUID alone.
        /// </remarks>
        [HttpPost]
        [Route("AddOrUpdateGlobalDataItem")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<ActionResult<SuccessAndErrorMessage>> AddOrUpdateGlobalDataItem([FromBody] AddOrUpdateGlobalDataItemDTO addOrUpdateGlobalDataItemDTO)
        {
            Request.Headers.TryGetValue(SamsaraServiceKeyCheck.Header, out var serviceKey);
            if (!SamsaraServiceKeyCheck.IsValid(serviceKey.ToString())) return Unauthorized();

            var addOrUpdateGlobalDataItemRequest = new AddOrUpdateGlobalDataItemRequest(
                addOrUpdateGlobalDataItemDTO,
                _globalDataRepository,
                _customerGuid);

            return await addOrUpdateGlobalDataItemRequest.Handle();
        }

        /// <summary>
        /// Gets an item from Global Data
        /// </summary>
        /// <remarks>
        /// Gets an item from the Global Data system by looking it up based on globalDataKey
        /// </remarks>
        [HttpGet]
        [Route("GetGlobalDataItem/{globalDataKey}")]
        [Produces(typeof(GlobalData))]
        public async Task<GlobalData> GetGlobalDataItem(string globalDataKey)
        {
            var getGlobalDataItemRequest = new GetGlobalDataItemRequest(
                globalDataKey,
                _globalDataRepository,
                _customerGuid);

            return await getGlobalDataItemRequest.Handle();
        }
    }
}