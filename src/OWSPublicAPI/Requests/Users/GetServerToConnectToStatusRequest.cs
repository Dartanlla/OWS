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
        public Guid UserSessionGUID { get; set; }
        public string CharacterName { get; set; }

        private Guid CustomerGUID;
        private IUsersRepository usersRepository;
        private ICharactersRepository charactersRepository;

        public void SetData(IUsersRepository usersRepository, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.usersRepository = usersRepository;
            this.charactersRepository = charactersRepository;
            CustomerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            var (character, error) = await PlayerAuthorization.CheckAsync(
                usersRepository, charactersRepository, CustomerGUID, UserSessionGUID, CharacterName);
            if (error != null)
                return error;

            if (MapInstanceID <= 0)
                return new BadRequestResult();

            var instance = await charactersRepository.CheckMapInstanceStatus(CustomerGUID, MapInstanceID);

            if (instance == null)
            {
                Log.Warning($"GetServerToConnectToStatus: MapInstanceID {MapInstanceID} not found.");
                return new NotFoundResult();
            }

            if (instance.Status == 2)
            {
                Log.Information($"GetServerToConnectToStatus: MapInstanceID {MapInstanceID} is ready. Connecting character {CharacterName}.");
                if (!await charactersRepository.AddOwnedCharacterToMapInstance(CustomerGUID, character.CharacterId, character.UserGuid.Value, MapInstanceID))
                    return new NotFoundResult();
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
