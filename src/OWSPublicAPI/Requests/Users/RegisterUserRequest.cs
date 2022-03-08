using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSExternalLoginProviders.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSPublicAPI.Requests.Users
{
    /// <summary>
    /// Register a User
    /// </summary>
    /// <remarks>
    /// Register a user with the system.  You can control validation with a custom IIPublicAPIInputValidation implementation.  See DefaultPublicAPIInputValidation for an example.
    /// </remarks>
    public class RegisterUserRequest : IRequestHandler<RegisterUserRequest, IActionResult>, IRequest
    {
        /// <summary>
        /// Email
        /// </summary>
        /// <remarks>
        /// Email for the user.  This value is not meant to be displayed in game.
        /// </remarks>
        public string Email { get; set; }
        /// <summary>
        /// Password
        /// </summary>
        /// <remarks>
        /// Password for the user.  Passwords are one way encrypted with SHA 256 and a 25 character Salt when using the MSSQL implementation of UsersRepository.
        /// </remarks>
        public string Password { get; set; }
        /// <summary>
        /// First Name
        /// </summary>
        /// <remarks>
        /// First Name for the user.  This value is not meant to be displayed in game.
        /// </remarks>
        public string FirstName { get; set; }
        /// <summary>
        /// Last Name
        /// </summary>
        /// <remarks>
        /// Last Name for the user.  This value is not meant to be displayed in game.
        /// </remarks>
        public string LastName { get; set; }

        private SuccessAndErrorMessage _output;
        private Guid _customerGUID;
        private IUsersRepository _usersRepository;
        private IExternalLoginProviderFactory _externalLoginProviderFactory;

        public void SetData(IUsersRepository usersRepository, IExternalLoginProviderFactory externalLoginProviderFactory, IHeaderCustomerGUID customerGuid)
        {
            _customerGUID = customerGuid.CustomerGUID;
            _usersRepository = usersRepository;
            _externalLoginProviderFactory = externalLoginProviderFactory;
        }

        public async Task<IActionResult> Handle()
        {
            //Check for duplicate account before creating a new one:
            var foundUser = await _usersRepository.GetUserFromEmail(_customerGUID, Email);

            //This user already exists
            if (foundUser != null)
            {
                _output = new SuccessAndErrorMessage();
                _output.Success = false;
                _output.ErrorMessage = "Duplicate Account!";

                return new OkObjectResult(_output);
            }

            _output = await _usersRepository.RegisterUser(_customerGUID, Email, Password, FirstName, LastName);

            /*
            if (externalLoginProviderFactory != null)
            {
                //This method will do nothing if AutoRegister isn't set to true
                //await externalLoginProvider.RegisterAsync(Email, Password, Email);
            }
            */

            return new OkObjectResult(_output);
        }
    }
}
