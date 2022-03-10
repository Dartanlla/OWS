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
using OWSInstanceManagement.Requests.Instance;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;
using System.Net.Http;
using OWSInstanceManagement.Requests.Zones;

namespace OWSInstanceManagement.Controllers
{
    /// <summary>
    /// Zones Controller
    /// </summary>
    /// <remarks>
    /// Controller for Handling Zone related requests.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ZonesController : Controller
    {
        private readonly Container _container;
        private readonly IInstanceManagementRepository _instanceManagementRepository;
        private readonly ICharactersRepository _charactersRepository;
        private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;
        private readonly IHeaderCustomerGUID _customerGuid;

        /// <summary>
        /// Zones Controller - OnActionExecuting
        /// </summary>
        /// <remarks>
        /// OnActionExecuting
        /// </remarks>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            IHeaderCustomerGUID customerGuid = _container.GetInstance<IHeaderCustomerGUID>();

            if (customerGuid.CustomerGUID == Guid.Empty)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        /// <summary>
        /// Zones Controller - Constructor
        /// </summary>
        /// <remarks>
        /// Setup for dependency injection.
        /// </remarks>
        public ZonesController(Container container,
            IInstanceManagementRepository instanceManagementRepository,
            ICharactersRepository charactersRepository,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            IHeaderCustomerGUID customerGuid)
        {
            _container = container;
            _instanceManagementRepository = instanceManagementRepository;
            _charactersRepository = charactersRepository;
            _rabbitMQOptions = rabbitMQOptions;
            _customerGuid = customerGuid;
        }

        /// <summary>
        /// Add a Zone
        /// </summary>
        /// <remarks>
        /// Add a Zone to the Maps table.
        /// </remarks>
        [HttpPost]
        [Route("AddZone")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<IActionResult> AddZone([FromBody] AddZoneRequest request)
        {
            request.SetData(_instanceManagementRepository, _customerGuid);

            return await request.Handle();
        }
    }
}
