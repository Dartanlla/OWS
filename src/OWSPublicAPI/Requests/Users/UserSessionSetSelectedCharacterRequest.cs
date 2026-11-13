using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Users
{
    public class UserSessionSetSelectedCharacterRequest : IRequestHandler<UserSessionSetSelectedCharacterRequest, IActionResult>, IRequest
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
            var (character, error) = await PlayerAuthorization.CheckAsync(
                usersRepository, charactersRepository, customerGUID, UserSessionGUID, SelectedCharacterName);
            if (error != null)
                return error;

            return new OkObjectResult(await usersRepository.UserSessionSetSelectedCharacter(customerGUID, UserSessionGUID, character.CharacterId));
        }
    }
}
