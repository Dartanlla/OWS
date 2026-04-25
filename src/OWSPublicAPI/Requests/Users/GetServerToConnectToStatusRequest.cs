using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace OWSPublicAPI.Requests.Users
{
    public class GetServerToConnectToStatusRequest : IRequestHandler<GetServerToConnectToStatusRequest, IActionResult>, IRequest
    {
        public int MapInstanceID { get; set; }
        public string CharacterName { get; set; }

        private Guid CustomerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            CustomerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            var instance = await charactersRepository.CheckMapInstanceStatus(CustomerGUID, MapInstanceID);

            if (instance == null)
            {
                Log.Warning($"GetServerToConnectToStatus: MapInstanceID {MapInstanceID} not found.");
                return new NotFoundResult();
            }

            if (instance.Status == 2)
            {
                Log.Information($"GetServerToConnectToStatus: MapInstanceID {MapInstanceID} is ready. Connecting character {CharacterName}.");
                await charactersRepository.AddCharacterToMapInstanceByCharName(CustomerGUID, CharacterName, MapInstanceID);
            }
            else
            {
                Log.Information($"GetServerToConnectToStatus: MapInstanceID {MapInstanceID} status is {instance.Status}. Still spinning up.");
            }

            return new OkObjectResult(new
            {
                MapInstanceStatus = instance.Status,
                Success = true,
                ErrorMessage = ""
            });
        }
    }
}
