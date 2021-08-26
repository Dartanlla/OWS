using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Users
{
    /// <summary>
    /// GetAllCharactersRequest
    /// </summary>
    /// <remarks>
    /// This request object handles requests for api/Users/GetAllCharacters
    /// </remarks>
    public class GetAllCharactersRequest
    {
        /// <summary>
        /// UserSessionGUID
        /// </summary>
        /// <remarks>
        /// This is the User Session GUID to determine the User to get all Characters for.
        /// </remarks>
        public Guid UserSessionGUID { get; set; }

        private IEnumerable<GetAllCharacters> output;
        private Guid customerGUID;
        private IUsersRepository usersRepository;

        /// <summary>
        /// SetData
        /// </summary>
        /// <remarks>
        /// Used to pass dependencies to the Request object (for performance reasons).
        /// </remarks>
        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        /// <summary>
        /// Handle
        /// </summary>
        /// <remarks>
        /// This handles the Request.
        /// </remarks>
        public async Task<IActionResult> Handle()
        {
            output = await usersRepository.GetAllCharacters(customerGUID, UserSessionGUID);

            return new OkObjectResult(output);
        }
    }
}
