using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Users
{
    public class SetSelectedCharacterAndGetUserSessionRequest : IRequestHandler<SetSelectedCharacterAndGetUserSessionRequest, IActionResult>, IRequest
    {   
        public Guid UserSessionGUID { get; set; }
        public string SelectedCharacterName { get; set; }

        private Guid customerGUID;
        private IUsersRepository usersRepository;
        private ICharactersRepository charactersRepository;

        public void SetData(IUsersRepository usersRepository, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
            this.charactersRepository = charactersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            var (_, error) = await UserSessionSetSelectedCharacterRequest.SelectAsync(
                usersRepository, charactersRepository, customerGUID, UserSessionGUID, SelectedCharacterName);
            if (error != null)
                return error;

            var session = await PlayerAuthorization.GetSessionAsync(usersRepository, customerGUID, UserSessionGUID);
            return session == null ? new UnauthorizedResult() : new OkObjectResult(session);
        }
    }
}
