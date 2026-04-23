using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    public class ShutDownServerInstanceRequest : IRequestHandler<SpinUpServerInstanceRequest, IActionResult>, IRequest
    {
        public int WorldServerID { get; set; }
        public int ZoneInstanceID { get; set; }

        private IInstanceLauncherMessaging _instanceLauncherMessaging;
        private IInstanceManagementRepository _instanceManagementRepository;
        private Guid CustomerGUID;

        public void SetData(IInstanceLauncherMessaging instanceLauncherMessaging, IInstanceManagementRepository instanceManagementRepository, IHeaderCustomerGUID customerGuid)
        {
            _instanceLauncherMessaging = instanceLauncherMessaging;
            _instanceManagementRepository = instanceManagementRepository;
            CustomerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            _instanceLauncherMessaging.PublishShutDownServerMessage(CustomerGUID, WorldServerID, ZoneInstanceID);

            return new OkObjectResult(new SuccessAndErrorMessage
            {
                Success = true,
                ErrorMessage = ""
            });
        }
    }
}
