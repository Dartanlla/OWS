using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.RequestPayloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    public class SetZoneInstanceStatusRequest : IRequestHandler<SetZoneInstanceStatusRequest, IActionResult>, IRequest
    {
        public SetZoneInstanceStatusRequestPayload request { get; set; }

        private IInstanceManagementRepository _instanceManagementRepository;
        private SuccessAndErrorMessage _output;
        private Guid _customerGUID;

        public void SetData(IInstanceManagementRepository instanceManagementRepository, IHeaderCustomerGUID customerGuid)
        {
            _instanceManagementRepository = instanceManagementRepository;
            _customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            await _instanceManagementRepository.SetZoneInstanceStatus(_customerGUID, request.ZoneInstanceID, request.InstanceStatus);

            _output = new SuccessAndErrorMessage()
            {
                Success = true,
                ErrorMessage = ""
            };

            return new OkObjectResult(_output);
        }
    }
}
