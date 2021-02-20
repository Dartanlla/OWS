using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Users
{
    public class LoginAndCreateSessionRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }

        private PlayerLoginAndCreateSession Output;
        private Guid CustomerGUID;
        private IUsersRepository usersRepository;

        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            //CustomerGUID = new Guid("56FB0902-6FE7-4BFE-B680-E3C8E497F016");
            this.CustomerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        public async Task<IActionResult> Run()
        {
            Output = await usersRepository.LoginAndCreateSession(CustomerGUID, Email, Password, false);

            if (!Output.Authenticated || !Output.UserSessionGuid.HasValue || Output.UserSessionGuid == Guid.Empty)
            {
                Output.ErrorMessage = "Username or Password is invalid!";
            }

            return new OkObjectResult(Output);
        }
    }
}
