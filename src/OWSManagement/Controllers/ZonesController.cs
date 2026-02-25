using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OWSData.Models.Composites;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSManagement.DTOs;
using OWSManagement.Requests.Zones;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZonesController : Controller
    {
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly IInstanceManagementRepository _zonesRepository;

        public ZonesController(IHeaderCustomerGUID customerGuid, IInstanceManagementRepository zonesRepository)
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
        /// Get all Zones
        /// </summary>
        /// <remarks>
        /// Gets a list of all Zones
        /// </remarks>
        [HttpGet]
        [Route("Get")]
        [Produces(typeof(IEnumerable<Maps>))]
        public async Task<IEnumerable<Maps>> Get()
        {
            GetZonesRequest getZonesRequest = new GetZonesRequest(_customerGuid.CustomerGUID, _zonesRepository);

            return await getZonesRequest.Handle();
        }

        /// <summary>
        /// Get all Zone Instances
        /// </summary>
        /// <remarks>
        /// Gets a list of all Zone Instances
        /// </remarks>
        [HttpGet]
        [Route("GetInstances")]
        [Produces(typeof(IEnumerable<MapInstances>))]
        public async Task<IEnumerable<MapInstances>> GetInstances()
        {
            GetMapInstancesRequest getMapInstancesRequest = new GetMapInstancesRequest(_customerGuid.CustomerGUID, _zonesRepository);

            return await getMapInstancesRequest.Handle();
        }
    }
}
