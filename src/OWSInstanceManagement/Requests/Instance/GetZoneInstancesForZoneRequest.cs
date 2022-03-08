using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.RequestPayloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    /// <summary>
    /// Get a list of Zone Instances for a Zone
    /// </summary>
    /// <remarks>
    /// Get a list of running Zone Instances for a Zone. This list can be used to allow players to pick which instance of a Zone they want to switch to.
    /// </remarks>
    public class GetZoneInstancesForZoneRequest
    {
        /// <summary>
        /// Request - ZoneName
        /// </summary>
        /// <remarks>
        /// This Request object contains a field for sending in the ZoneName to get a list of running Zone Instances for.
        /// </remarks>
        public ZoneNameRequestPayload Request { get; set; }

        private IEnumerable<GetZoneInstancesForZone> _output;
        private IInstanceManagementRepository _instanceManagementRepository;
        private Guid _customerGUID;

        public void SetData(IInstanceManagementRepository instanceManagementRepository, IHeaderCustomerGUID customerGuid)
        {
            _instanceManagementRepository = instanceManagementRepository;
            _customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            _output = await _instanceManagementRepository.GetZoneInstancesOfZone(_customerGUID, Request.ZoneName);

            return new OkObjectResult(_output);
        }
    }
}
