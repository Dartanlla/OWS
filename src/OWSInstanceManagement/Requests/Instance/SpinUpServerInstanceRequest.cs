using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    public class SpinUpServerInstanceRequest : IRequestHandler<SpinUpServerInstanceRequest, IActionResult>, IRequest
    {
        public int WorldServerID { get; set; }
        public int ZoneInstanceID { get; set; }
        public string ZoneName { get; set; }
        public int Port { get; set; }

        private IInstanceLauncherMessaging _instanceLauncherMessaging;
        private ICharactersRepository _charactersRepository;
        private Guid CustomerGUID;

        public void SetData(IInstanceLauncherMessaging instanceLauncherMessaging, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            _instanceLauncherMessaging = instanceLauncherMessaging;
            _charactersRepository = charactersRepository;
            CustomerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            _instanceLauncherMessaging.PublishSpinUpServerMessage(CustomerGUID, WorldServerID, ZoneInstanceID, ZoneName, Port);

            return new OkObjectResult(new OWSData.Models.Composites.SuccessAndErrorMessage
            {
                Success = true,
                ErrorMessage = ""
            });
        }
    }
}
