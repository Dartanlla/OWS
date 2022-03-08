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
    /// GetPlayerGroupsCharacterIsInRequest
    /// </summary>
    /// <remarks>
    /// This request object handles requests for api/Users/GetPlayerGroupsCharacterIsIn
    /// </remarks>
    public class GetPlayerGroupsCharacterIsInRequest : IRequestHandler<GetPlayerGroupsCharacterIsInRequest, IActionResult>, IRequest
    {
        /// <summary>
        /// UserSessionGUID
        /// </summary>
        /// <remarks>
        /// This is the User Session GUID to determine the User to get Player Groups for.
        /// </remarks>
        public Guid UserSessionGUID { get; set; }
        /// <summary>
        /// CharacterName
        /// </summary>
        /// <remarks>
        /// CharacterName filters the list of Player Groups to only return for this CharacterName.
        /// </remarks>
        public string CharacterName { get; set; }
        /// <summary>
        /// PlayerGroupTypeID
        /// </summary>
        /// <remarks>
        /// PlayerGroupTypeID filters the list of Player Groups to only return for this PlayerGroupTypeID.  Set this parameter to zero to remove the Player Group Type filter.
        /// </remarks>
        public int PlayerGroupTypeID { get; set; }

        private IEnumerable<GetPlayerGroupsCharacterIsIn> output;
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
            output = await usersRepository.GetPlayerGroupsCharacterIsIn(customerGUID, UserSessionGUID, CharacterName, PlayerGroupTypeID);

            return new OkObjectResult(output);
        }
    }
}
