using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSData.Models.StoredProcs;

namespace OWSPublicAPI.Requests.Users
{
    public class SetSelectedCharacterAndGetUserSessionRequest : IRequestHandler<SetSelectedCharacterAndGetUserSessionRequest, IActionResult>, IRequest
    {   
        public Guid UserSessionGUID { get; set; }
        public string SelectedCharacterName { get; set; }

        private GetUserSession output;
        private SuccessAndErrorMessage successOrError;
        private Guid customerGUID;
        private IUsersRepository usersRepository;

        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            successOrError = await usersRepository.UserSessionSetSelectedCharacter(customerGUID, UserSessionGUID, SelectedCharacterName);

            output = await usersRepository.GetUserSession(customerGUID, UserSessionGUID);

            return new OkObjectResult(output);
        }
    }
}
