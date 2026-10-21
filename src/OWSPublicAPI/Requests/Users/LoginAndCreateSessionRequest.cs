using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using Serilog;

namespace OWSPublicAPI.Requests.Users
{
    public class LoginAndCreateSessionRequest : IRequestHandler<LoginAndCreateSessionRequest, IActionResult>, IRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }

        private PlayerLoginAndCreateSession output;
        private Guid customerGUID;
        private IUsersRepository usersRepository;

        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            //CustomerGUID = new Guid("56FB0902-6FE7-4BFE-B680-E3C8E497F016");
            this.customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            output = await usersRepository.LoginAndCreateSession(customerGUID, Email, Password, false);

            if (output == null)
            {
                output = new PlayerLoginAndCreateSession
                {
                    Authenticated = false,
                    ErrorMessage = "Login service unavailable"
                };
            }

            bool loggedIn = output.Authenticated && output.UserSessionGuid.HasValue && output.UserSessionGuid != Guid.Empty;
            if (!loggedIn && string.IsNullOrWhiteSpace(output.ErrorMessage))
            {
                output.ErrorMessage = "Invalid email or password";
            }

            // No email in the log: the session GUID leads to the account in the database.
            Log.Information("user_login {Method} {Result} session {UserSessionGuid}", "password", loggedIn ? "ok" : "refused", output.UserSessionGuid);
            return new OkObjectResult(output);
        }
    }
}
