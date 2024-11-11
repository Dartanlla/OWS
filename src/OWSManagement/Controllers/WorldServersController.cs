using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OWSData.Models.Composites;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSManagement.DTOs;
using OWSManagement.Requests.Worlds;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorldServersController : Controller
    {
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly IInstanceManagementRepository _zonesRepository;

        public WorldServersController(IHeaderCustomerGUID customerGuid, IInstanceManagementRepository zonesRepository)
        {
            _customerGuid = customerGuid;
            _zonesRepository = zonesRepository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_customerGuid.CustomerGUID == Guid.Empty)
            {
                context.Result = new UnauthorizedResult();
            }
        }


        /// <summary>
        /// Get all WorldServers
        /// </summary>
        /// <remarks>
        /// Gets a list of all WorldServers
        /// </remarks>
        [HttpGet]
        [Route("Get")]
        [Produces(typeof(IEnumerable<WorldServers>))]
        public async Task<IEnumerable<WorldServers>> Get()
        {
            GetWorldServersRequest getWorldServersRequest = new GetWorldServersRequest(_customerGuid.CustomerGUID, _zonesRepository);

            return await getWorldServersRequest.Handle();
        }
    }
}
