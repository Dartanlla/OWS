using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    public class StartInstanceLauncherRequest : IRequestHandler<StartInstanceLauncherRequest, IActionResult>, IRequest
    {
        private int _output;
        private string _launcherGuid;
        private IInstanceManagementRepository _instanceManagementRepository;
        private Guid _customerGUID;

        public void SetData(IInstanceManagementRepository instanceManagementRepository, string launcherGuid, IHeaderCustomerGUID customerGuid)
        {
            _instanceManagementRepository = instanceManagementRepository;

            _launcherGuid = launcherGuid;
            _customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            _output = await _instanceManagementRepository.StartWorldServer(_customerGUID, _launcherGuid);

            return new OkObjectResult(_output);
        }
    }
}
