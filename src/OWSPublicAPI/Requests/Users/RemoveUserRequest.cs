using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSPublicAPI.Requests.Users
{
    /// <summary>
    /// RemoveUserRequest Handler
    /// </summary>
    /// <remarks>
    /// Handles api/Users/RemoveUser requests.
    /// </remarks>
    public class RemoveUserRequest
    {
        /// <summary>
        /// UserGUID Request Paramater.
        /// </summary>
        /// <remarks>
        /// Contains the User GUID from the request.  This identifies the User we are modifying.
        /// </remarks>
        public Guid UserGUID { get; set; }
        /// <summary>
        /// Email Request Paramater.
        /// </summary>
        /// <remarks>
        /// Contains the Email from the request.
        /// </remarks>
        public string Email { get; set; }

        private Guid CustomerGUID;
        private IUsersRepository usersRepository;
        private IPublicAPIInputValidation publicAPIInputValidation;

        /// <summary>
        /// Set Dependencies for RemoveUserRequest
        /// </summary>
        /// <remarks>
        /// Injects the dependencies for the RemoveUserRequest.
        /// </remarks>
        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            CustomerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        /// <summary>
        /// Handles the RemoveUser Request
        /// </summary>
        /// <remarks>
        /// Overrides IRequestHandler Handle().
        /// </remarks>
        public async Task<IActionResult> Handle()
        {
            SuccessAndErrorMessage output;

            output = await usersRepository.RemoveUser(CustomerGUID, UserGUID, Email);

            return new OkObjectResult(output);
        }
    }
}
