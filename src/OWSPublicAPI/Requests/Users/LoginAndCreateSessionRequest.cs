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
        private IAuditRepository auditRepository;

        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid, IAuditRepository auditRepository)
        {
            //CustomerGUID = new Guid("56FB0902-6FE7-4BFE-B680-E3C8E497F016");
            this.customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
            this.auditRepository = auditRepository;
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

            // No email in the log, which leaves the box; the audit row, in our own database, keeps it.
            string result = loggedIn ? "ok" : "refused";
            Log.Information("user_login {Method} {Result} session {UserSessionGuid}", "password", result, output.UserSessionGuid);
            await auditRepository.Add(customerGUID, "publicapi", Email, "user_login", "password",
                new { method = "password", result, userSessionGuid = output.UserSessionGuid });
            return new OkObjectResult(output);
        }
    }
}
