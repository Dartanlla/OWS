using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSManagement.DTOs;
using OWSManagement.Requests.Users;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly IUsersRepository _usersRepository;

        public UsersController(IHeaderCustomerGUID customerGuid, IUsersRepository usersRepository)
        {
            _customerGuid = customerGuid;
            _usersRepository = usersRepository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_customerGuid.CustomerGUID == Guid.Empty)
            {
                context.Result = new UnauthorizedResult();
            }
        }


        /// <summary>
        /// Search Users
        /// </summary>
        /// <remarks>
        /// Finds users in this CustomerGUID whose email, first name, last name or SteamId
        /// contains the search text. An empty search returns the first page. Capped at 200
        /// rows, so this no longer returns the whole table the way the old unbounded list did.
        /// </remarks>
        [HttpGet]
        [Route("")]
        [Produces(typeof(IEnumerable<AdminUserSummary>))]
        public async Task<IEnumerable<AdminUserSummary>> Get([FromQuery] string search)
        {
            SearchUsersRequest searchUsersRequest = new SearchUsersRequest(_customerGuid.CustomerGUID, search, _usersRepository);

            return await searchUsersRequest.Handle();
        }

        /// <summary>
        /// Get the valid Roles
        /// </summary>
        /// <remarks>
        /// Returns the Users.Role values the API accepts, so the console does not have to
        /// hardcode them.
        /// </remarks>
        [HttpGet]
        [Route("Roles")]
        [Produces(typeof(IEnumerable<string>))]
        public IEnumerable<string> Roles()
        {
            return UserRoles.All;
        }

        /// <summary>
        /// Add a User
        /// </summary>
        /// <remarks>
        /// Adds a new user
        /// </remarks>
        [Authorize(Policy = Auth.ConsoleAuth.Admin)]
        [HttpPost]
        [Route("")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> Post([FromBody] AddUserDTO addUserDTO)
        {
            AddUserRequest addUserRequest = new AddUserRequest(_customerGuid.CustomerGUID, addUserDTO, _usersRepository);

            return await addUserRequest.Handle();
        }

        /// <summary>
        /// Set the network test flag for a whole account
        /// </summary>
        /// <remarks>
        /// Sets Users.IsInternalNetworkTestUser, which makes GetServerToConnectTo hand every
        /// character of this user, new ones included, 127.0.0.1 instead of the zone server's
        /// real IP. Characters.IsInternalNetworkTestUser still flags one character on its own.
        /// </remarks>
        [Authorize(Policy = Auth.ConsoleAuth.Admin)]
        [HttpPut]
        [Route("NetworkTestFlag")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> SetNetworkTestFlag([FromBody] SetUserNetworkTestFlagDTO dto)
        {
            SetUserNetworkTestFlagRequest request = new SetUserNetworkTestFlagRequest(_customerGuid.CustomerGUID, dto, _usersRepository);

            return await request.Handle();
        }

        /// <summary>
        /// Edit a User
        /// </summary>
        /// <remarks>
        /// Edit an existing user.  Don't allow editing of the password.
        /// </remarks>
        [Authorize(Policy = Auth.ConsoleAuth.Admin)]
        [HttpPut]
        [Route("")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> Put([FromBody] EditUserDTO editUserDTO)
        {
            EditUserRequest editUserRequest = new EditUserRequest(_customerGuid.CustomerGUID, editUserDTO, _usersRepository);

            return await editUserRequest.Handle();
        }
    }
}
