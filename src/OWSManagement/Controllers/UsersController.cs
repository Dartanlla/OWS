using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
        /// Get all Users
        /// </summary>
        /// <remarks>
        /// Gets a list of all users for this CustomerGUID
        /// </remarks>
        [HttpGet]
        [Route("")]
        [Produces(typeof(IEnumerable<User>))]
        public async Task<IEnumerable<User>> Get()
        {
            GetUsersRequest getUsersRequest = new GetUsersRequest(_customerGuid.CustomerGUID, _usersRepository);

            return await getUsersRequest.Handle();
        }

        /// <summary>
        /// Add a User
        /// </summary>
        /// <remarks>
        /// Adds a new user
        /// </remarks>
        [HttpPost]
        [Route("")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> Post([FromBody] AddUserDTO addUserDTO)
        {
            AddUserRequest addUserRequest = new AddUserRequest(_customerGuid.CustomerGUID, addUserDTO, _usersRepository);

            return await addUserRequest.Handle();
        }

        /// <summary>
        /// Edit a User
        /// </summary>
        /// <remarks>
        /// Edit an existing user.  Don't allow editing of the password.
        /// </remarks>
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
