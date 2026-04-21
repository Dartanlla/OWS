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

        private SuccessAndErrorMessage output;
        private Guid customerGUID;
        private IUsersRepository usersRepository;

        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            var userCharacters = await usersRepository.GetAllCharacters(customerGUID, UserSessionGUID);
            bool characterExistsForUser = userCharacters.Any(character =>
                !string.IsNullOrWhiteSpace(character.CharName) &&
                string.Equals(character.CharName, SelectedCharacterName, StringComparison.OrdinalIgnoreCase));

            if (!characterExistsForUser)
            {
                return new OkObjectResult(new SuccessAndErrorMessage
                {
                    Success = false,
                    ErrorMessage = "SelectedCharacterName does not exist for this user session."
                });
            }

            output = await usersRepository.UserSessionSetSelectedCharacter(customerGUID, UserSessionGUID, SelectedCharacterName);

            return new OkObjectResult(output);
        }
    }
}
