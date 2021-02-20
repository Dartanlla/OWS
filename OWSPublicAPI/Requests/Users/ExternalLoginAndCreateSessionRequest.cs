using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSData.Models.StoredProcs;
using OWSExternalLoginProviders.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace OWSPublicAPI.Requests.Users
{
    public class ExternalLoginAndCreateSessionRequest
    {   
        public string Email { get; set; }
        public string Password { get; set; }

        private PlayerLoginAndCreateSession Output;
        private Guid CustomerGUID;
        private IUsersRepository usersRepository;
        private IExternalLoginProvider externalLoginProvider;

        public void SetData(IUsersRepository usersRepository, IExternalLoginProvider externalLoginProvider, IHeaderCustomerGUID customerGuid)
        {
            //CustomerGUID = new Guid("56FB0902-6FE7-4BFE-B680-E3C8E497F016");
            this.CustomerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
            this.externalLoginProvider = externalLoginProvider;
        }

        public async Task<IActionResult> Run()
        {
            //Call external provider to get token
            string token = await externalLoginProvider.AuthenticateAsync(Email, Password, false);

            if (!String.IsNullOrEmpty(token) && externalLoginProvider.ValidateLoginToken(token, Email))
            {
                //Login to OWS
                Output = await usersRepository.LoginAndCreateSession(CustomerGUID, Email, Password);

                if (!Output.Authenticated || !Output.UserSessionGuid.HasValue || Output.UserSessionGuid == Guid.Empty)
                {
                    Output.ErrorMessage = "Username or Password is invalid!";
                }

                return new OkObjectResult(Output);
            }

            //Not authenticated
            Output = new PlayerLoginAndCreateSession();
            Output.Authenticated = false;
            Output.UserSessionGuid = Guid.Empty;
            Output.ErrorMessage = externalLoginProvider.GetErrorFromToken(token);
            return new OkObjectResult(Output);
        }
    }
}
