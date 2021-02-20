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
    public class SetSelectedCharacterAndGetUserSessionRequest
    {   public Guid UserSessionGUID { get; set; }
        public string SelectedCharacterName { get; set; }

        private GetUserSession Output;
        private SuccessAndErrorMessage SuccessOrError;
        private Guid CustomerGUID;
        private IUsersRepository usersRepository;

        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            CustomerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        public async Task<IActionResult> Run()
        {
            SuccessOrError = await usersRepository.UserSessionSetSelectedCharacter(CustomerGUID, UserSessionGUID, SelectedCharacterName);

            Output = await usersRepository.GetUserSession(CustomerGUID, UserSessionGUID);

            return new OkObjectResult(Output);
        }
    }
}
