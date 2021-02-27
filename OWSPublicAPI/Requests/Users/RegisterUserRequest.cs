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

        private SuccessAndErrorMessage output;
        private Guid customerGUID;
        private IUsersRepository usersRepository;
        private IExternalLoginProvider externalLoginProvider;

        public void SetData(IUsersRepository usersRepository, IExternalLoginProvider externalLoginProvider, IHeaderCustomerGUID customerGuid)
        {
            customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
            this.externalLoginProvider = externalLoginProvider;
        }

        public async Task<IActionResult> Handle()
        {
            //Check for duplicate account before creating a new one:
            var foundUser = await usersRepository.GetUserFromEmail(customerGUID, Email);

            //This user already exists
            if (foundUser != null)
            {
                output = new SuccessAndErrorMessage();
                output.Success = false;
                output.ErrorMessage = "Duplicate Account!";

                return new OkObjectResult(output);
            }

            output = await usersRepository.RegisterUser(customerGUID, Email, Password, FirstName, LastName);

            if (externalLoginProvider != null)
            {
                //This method will do nothing if AutoRegister isn't set to true
                await externalLoginProvider.RegisterAsync(Email, Password, Email);
            }

            return new OkObjectResult(output);
        }
    }
}
