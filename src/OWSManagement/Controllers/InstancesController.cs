using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Controllers
{
    /// <summary>
    /// Live zone instances (MapInstances rows) for the management console.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InstancesController : Controller
    {
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly IInstanceManagementRepository _instanceManagementRepository;

        public InstancesController(IHeaderCustomerGUID customerGuid, IInstanceManagementRepository instanceManagementRepository)
        {
            _customerGuid = customerGuid;
            _instanceManagementRepository = instanceManagementRepository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_customerGuid.CustomerGUID == Guid.Empty)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        /// <summary>
        /// List Live Zone Instances
        /// </summary>
        /// <remarks>
        /// Every MapInstances row for this CustomerGUID with its zone, world server region, the load its server
        /// last reported in the heartbeat, and the characters CharOnMapInstance places on it.
        /// </remarks>
        [HttpGet]
        [Route("")]
        [Produces(typeof(IEnumerable<ZoneInstanceSummary>))]
        public async Task<IEnumerable<ZoneInstanceSummary>> Get()
        {
            return await _instanceManagementRepository.GetLiveZoneInstances(_customerGuid.CustomerGUID);
        }
    }
}
