using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.RequestPayloads;
using System;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    public class RegisterInstanceLauncherRequest : IRequestHandler<RegisterInstanceLauncherRequest, SuccessAndErrorMessage>, IRequest
    {
        // private SuccessAndErrorMessage Output;

        private IInstanceManagementRepository _instanceManagementRepository;
        private Guid _customerGUID;
        private SuccessAndErrorMessage _output;
        public RegisterInstanceLauncherRequestPayload Request { get; set; }

        public void SetData(IInstanceManagementRepository instanceManagementRepository, IHeaderCustomerGUID customerGuid)
        {
            _instanceManagementRepository = instanceManagementRepository;
            _customerGUID = customerGuid.CustomerGUID;

        }

        public async Task<SuccessAndErrorMessage> Handle()
        {
            _output = await _instanceManagementRepository.RegisterLauncher(_customerGUID, Request.launcherGUID, Request.ServerIP, Request.MaxNumberOfInstances, Request.InternalServerIP, Request.StartingInstancePort);

            return _output;
        }
    }
}