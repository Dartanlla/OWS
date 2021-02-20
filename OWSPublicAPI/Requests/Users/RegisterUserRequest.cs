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
    public class RegisterUserRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        private SuccessAndErrorMessage Output;
        private Guid CustomerGUID;
        private IUsersRepository usersRepository;
        private IExternalLoginProvider externalLoginProvider;

        public void SetData(IUsersRepository usersRepository, IExternalLoginProvider externalLoginProvider, IHeaderCustomerGUID customerGuid)
        {
            CustomerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
            this.externalLoginProvider = externalLoginProvider;
        }

        public async Task<IActionResult> Run()
        {
            //Check for duplicate account before creating a new one:
            var foundUser = await usersRepository.GetUserFromEmail(CustomerGUID, Email);

            //This user already exists
            if (foundUser != null)
            {
                Output = new SuccessAndErrorMessage();
                Output.Success = false;
                Output.ErrorMessage = "Duplicate Account!";

                return new OkObjectResult(Output);
            }

            Output = await usersRepository.RegisterUser(CustomerGUID, Email, Password, FirstName, LastName);

            if (externalLoginProvider != null)
            {
                //This method will do nothing if AutoRegister isn't set to true
                await externalLoginProvider.RegisterAsync(Email, Password, Email);
            }

            return new OkObjectResult(Output);
        }
    }
}
