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
    public class RegisterUserRequest : IRequestHandler<RegisterUserRequest, IActionResult>, IRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
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
