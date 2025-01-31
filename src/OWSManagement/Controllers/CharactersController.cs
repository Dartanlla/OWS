using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OWSData.Models.Composites;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSManagement.DTOs;
using OWSManagement.Requests.PlayerCharacters;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CharactersController : Controller
    {
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly ICharactersRepository _charactersRepository;

        public CharactersController(IHeaderCustomerGUID customerGuid, ICharactersRepository charactersRepository)
        {
            _customerGuid = customerGuid;
            _charactersRepository = charactersRepository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_customerGuid.CustomerGUID == Guid.Empty)
            {
                context.Result = new UnauthorizedResult();
            }
        }


        /// <summary>
        /// Get all Characters
        /// </summary>
        /// <remarks>
        /// Gets a list of all Characters
        /// </remarks>
        [HttpGet]
        [Route("Get")]
        [Produces(typeof(IEnumerable<Characters>))]
        public async Task<IEnumerable<Characters>> Get()
        {
            GetCharactersRequest getCharactersRequest = new GetCharactersRequest(_customerGuid.CustomerGUID, _charactersRepository);

            return await getCharactersRequest.Handle();
        }
    }
}
