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
    public class GetZoneInstancesForWorldServerRequest : IRequestHandler<GetServerToConnectToRequest, IActionResult>, IRequest
    {
        public WorldServerIDRequestPayload Request { get; set; }

        private IEnumerable<GetZoneInstancesForWorldServer> _output;
        private IInstanceManagementRepository _instanceManagementRepository;
        private Guid _customerGUID;

        public void SetData(IInstanceManagementRepository instanceManagementRepository, IHeaderCustomerGUID customerGuid)
        {
            _instanceManagementRepository = instanceManagementRepository;
            _customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            _output = await _instanceManagementRepository.GetZoneInstancesForWorldServer(_customerGUID, Request.WorldServerID);

            return new OkObjectResult(_output);
        }
    }
}
