using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSExternalLoginProviders.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;
using OWSPublicAPI.DTOs;

namespace OWSPublicAPI.Requests.Users
{
    /// <summary>
    /// Register a User
    /// </summary>
    /// <remarks>
    /// Register a user with the system.  You can control validation with a custom IIPublicAPIInputValidation implementation.  See DefaultPublicAPIInputValidation for an example.
    /// </remarks>
    public class RegisterUserRequest
    {
        private readonly RegisterUserDTO _registerUserDTO;
        private readonly Guid _customerGUID;
        private readonly IUsersRepository _usersRepository;
        private readonly IExternalLoginProviderFactory _externalLoginProviderFactory;
        private readonly IPublicAPIInputValidation _publicAPIInputValidation;

        /// <summary>
        /// RegisterUserRequest Constructor
        /// </summary>
        /// <remarks>
        /// Initialize the RegisterUserRequest object with dependencies
        /// </remarks>
        public RegisterUserRequest(RegisterUserDTO registerUserDTO, IUsersRepository usersRepository, IExternalLoginProviderFactory externalLoginProviderFactory, IHeaderCustomerGUID customerGuid,
            IPublicAPIInputValidation publicAPIInputValidation)
        {
            _registerUserDTO = registerUserDTO;
            _customerGUID = customerGuid.CustomerGUID;
            _usersRepository = usersRepository;
            _externalLoginProviderFactory = externalLoginProviderFactory;
            _publicAPIInputValidation = publicAPIInputValidation;
        }

        /// <summary>
        /// RegisterUserRequest Request Handler
        /// </summary>
        /// <remarks>
        /// Handle the RegisterUserRequest request
        /// </remarks>
        public async Task<PlayerLoginAndCreateSession> Handle()
        {
            //Validate Email Address
            string errorMessage = _publicAPIInputValidation.ValidateEmail(_registerUserDTO.Email);

            //Check for duplicate email address before creating a new account:
            var foundUser = await _usersRepository.GetUserFromEmail(_customerGUID, _registerUserDTO.Email);

            //This user already exists
            if (foundUser != null)
            {
                PlayerLoginAndCreateSession errorOutput = new PlayerLoginAndCreateSession()
                {
                    ErrorMessage = "Duplicate Email Address!"
                };

                return errorOutput;
            }

            //Validate password
            errorMessage = _publicAPIInputValidation.ValidatePassword(_registerUserDTO.Password);

            //If any of the validation checks found issues
            if (!String.IsNullOrEmpty(errorMessage))
            {
                PlayerLoginAndCreateSession errorOutput = new PlayerLoginAndCreateSession();
                errorOutput.ErrorMessage = errorMessage;
                return errorOutput;
            }

            //Register the new account
            SuccessAndErrorMessage registerOutput = await _usersRepository.RegisterUser(_customerGUID, _registerUserDTO.Email, _registerUserDTO.Password, _registerUserDTO.FirstName, _registerUserDTO.LastName);

            //There was an error registering the new account
            if (!registerOutput.Success)
            {
                PlayerLoginAndCreateSession errorOutput = new PlayerLoginAndCreateSession()
                {
                    ErrorMessage = registerOutput.ErrorMessage
                };

                return errorOutput;
            }

            //Login to the new account to get a UserSession
            PlayerLoginAndCreateSession playerLoginAndCreateSession = await _usersRepository.LoginAndCreateSession(_customerGUID, _registerUserDTO.Email, _registerUserDTO.Password);

            /*
            if (externalLoginProviderFactory != null)
            {
                //This method will do nothing if AutoRegister isn't set to true
                //await externalLoginProvider.RegisterAsync(Email, Password, Email);
            }
            */

            return playerLoginAndCreateSession;
        }
    }
}
