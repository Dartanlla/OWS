using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OWSData.Models.Composites;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSManagement.Requests.Users;
using OWSShared.Interfaces;

namespace OWSManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IHeaderCustomerGUID _customerGuid;
        public UsersController(IHeaderCustomerGUID customerGuid)
        {
            _customerGuid = customerGuid;
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
        [Route("/")]
        [Produces(typeof(List<User>))]
        public async Task<List<User>> Get()
        {
            GetUsersRequest getUsersRequest = new GetUsersRequest(_customerGuid.CustomerGUID);

            return await getUsersRequest.Handle();
        }
    }
}
