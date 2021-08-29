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
    /// <summary>
    /// ExternalLoginAndCreateSessionRequest Handler
    /// </summary>
    /// <remarks>
    /// Handles api/Users/ExternalLoginAndCreateSession requests.
    /// </remarks>
    public class ExternalLoginAndCreateSessionRequest : IRequestHandler<ExternalLoginAndCreateSessionRequest, IActionResult>, IRequest
    {
        /// <summary>
        /// Email Request Paramater.
        /// </summary>
        /// <remarks>
        /// The email is used as the primary ID for the sign in credentials.
        /// </remarks>
        public string Email { get; set; }
        /// <summary>
        /// Password Request Paramater.
        /// </summary>
        /// <remarks>
        /// The password is part of the sign in credentials.
        /// </remarks>
        public string Password { get; set; }

        private PlayerLoginAndCreateSession _output;
        private Guid _customerGUID;
        private IUsersRepository _usersRepository;
        private IExternalLoginProviderFactory _externalLoginProviderFactory;

        /// <summary>
        /// Set Dependencies for ExternalLoginAndCreateSessionRequest
        /// </summary>
        /// <remarks>
        /// Injects the dependencies for the ExternalLoginAndCreateSessionRequest.
        /// </remarks>
        public void SetData(IUsersRepository usersRepository, IExternalLoginProviderFactory externalLoginProviderFactory, IHeaderCustomerGUID customerGuid)
        {
            _customerGUID = customerGuid.CustomerGUID;
            _usersRepository = usersRepository;
            _externalLoginProviderFactory = externalLoginProviderFactory;
        }

        public async Task<IActionResult> Handle()
        {
            //Call external provider to get token
            /*
            string token = await externalLoginProvider.AuthenticateAsync(Email, Password, false);

            if (!String.IsNullOrEmpty(token) && externalLoginProvider.ValidateLoginToken(token, Email))
            {
                //Login to OWS
                output = await usersRepository.LoginAndCreateSession(customerGUID, Email, Password);

                if (!output.Authenticated || !output.UserSessionGuid.HasValue || output.UserSessionGuid == Guid.Empty)
                {
                    output.ErrorMessage = "Username or Password is invalid!";
                }

                return new OkObjectResult(output);
            }
            */

            //Not authenticated
            _output = new PlayerLoginAndCreateSession();
            _output.Authenticated = false;
            _output.UserSessionGuid = Guid.Empty;
            //output.ErrorMessage = externalLoginProvider.GetErrorFromToken(token);
            return new OkObjectResult(_output);
            
        }
    }
}
