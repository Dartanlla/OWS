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
        private string _ip;
        private IInstanceManagementRepository _instanceManagementRepository;
        private Guid _customerGUID;

        public void SetData(IInstanceManagementRepository instanceManagementRepository, string ip, IHeaderCustomerGUID customerGuid)
        {
            _instanceManagementRepository = instanceManagementRepository;

            //If running locally in docker the IP will contain "::", so switch it to 127.0.0.1 as this is running locally
            if (ip.Contains("::"))
            {
                _ip = "127.0.0.1";
            }
            else
            {
                _ip = ip;
            }

            _customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            _output = await _instanceManagementRepository.StartWorldServer(_customerGUID, _ip);

            return new OkObjectResult(_output);
        }
    }
}
