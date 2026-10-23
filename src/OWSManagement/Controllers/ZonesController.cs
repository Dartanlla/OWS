using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSManagement.DTOs;
using OWSManagement.Requests.Zones;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Controllers
{
    /// <summary>
    /// Zone (Maps table) administration for the management console.
    /// </summary>
    /// <remarks>
    /// OWSInstanceManagement also exposes an AddZone endpoint, but it is reached by the game
    /// services rather than an operator and has no list, edit or delete. This controller talks
    /// to the repository directly, the same way UsersController does, so the console needs no
    /// server-to-server API key to manage zones.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ZonesController : Controller
    {
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly IInstanceManagementRepository _instanceManagementRepository;

        public ZonesController(IHeaderCustomerGUID customerGuid, IInstanceManagementRepository instanceManagementRepository)
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
        /// List Zones
        /// </summary>
        /// <remarks>
        /// Every row of the Maps table for this CustomerGUID, with a count of the MapInstances
        /// pointing at each. MapData is not returned.
        /// </remarks>
        [HttpGet]
        [Route("")]
        [Produces(typeof(IEnumerable<ZoneSummary>))]
        public async Task<IEnumerable<ZoneSummary>> Get()
        {
            GetZonesRequest request = new GetZonesRequest(_customerGuid.CustomerGUID, _instanceManagementRepository);

            return await request.Handle();
        }

        /// <summary>
        /// Add a Zone
        /// </summary>
        /// <remarks>
        /// Adds a row to the Maps table. This registers the zone with OWS only - the packaged
        /// server build still has to contain a level matching MapName, or spin-up will fail.
        /// A ZoneName already in use by another row is rejected.
        /// </remarks>
        [Authorize(Policy = Auth.ConsoleAuth.Admin)]
        [HttpPost]
        [Route("")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> Post([FromBody] AddOrUpdateZoneDTO dto)
        {
            AddZoneRequest request = new AddZoneRequest(_customerGuid.CustomerGUID, dto, _instanceManagementRepository);

            return await request.Handle();
        }

        /// <summary>
        /// Edit a Zone
        /// </summary>
        /// <remarks>
        /// Updates an existing Maps row by MapID. Renaming onto another row's ZoneName is rejected.
        /// </remarks>
        [Authorize(Policy = Auth.ConsoleAuth.Admin)]
        [HttpPut]
        [Route("")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> Put([FromBody] AddOrUpdateZoneDTO dto)
        {
            UpdateZoneRequest request = new UpdateZoneRequest(_customerGuid.CustomerGUID, dto, _instanceManagementRepository);

            return await request.Handle();
        }

        /// <summary>
        /// Delete a Zone
        /// </summary>
        /// <remarks>
        /// Removes a Maps row. Refused while MapInstances rows still point at it, since nothing
        /// in the schema cascades and the launcher would be left unable to resolve their name.
        /// </remarks>
        [Authorize(Policy = Auth.ConsoleAuth.Admin)]
        [HttpDelete]
        [Route("{mapId:int}")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> Delete(int mapId)
        {
            DeleteZoneRequest request = new DeleteZoneRequest(_customerGuid.CustomerGUID, mapId, _instanceManagementRepository);

            return await request.Handle();
        }
    }
}
