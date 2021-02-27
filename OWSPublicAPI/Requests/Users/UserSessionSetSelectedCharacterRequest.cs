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
            output = await usersRepository.UserSessionSetSelectedCharacter(customerGUID, UserSessionGUID, SelectedCharacterName);

            return new OkObjectResult(output);
        }
    }
}
