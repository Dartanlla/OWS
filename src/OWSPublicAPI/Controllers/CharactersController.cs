using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Data;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using SimpleInjector;
using OWSData.Models.StoredProcs;
using OWSShared.Interfaces;
using OWSPublicAPI.Requests.Characters;
using OWSData.Repositories.Interfaces;

namespace OWSPublicAPI.Controllers
{
    /// <summary>
    /// Public Character related API calls.
    /// </summary>
    /// <remarks>
    /// Contains character related API calls that are all publicly accessible.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class CharactersController : Controller
    {
        private readonly Container _container;
        private readonly ICharactersRepository _charactersRepository;
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly ICustomCharacterDataSelector _customCharacterDataSelector;

        /// <summary>
        /// Constructor for Public Character related API calls.
        /// </summary>
        /// <remarks>
        /// All dependencies are injected.
        /// </remarks>
        public CharactersController(Container container, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, ICustomCharacterDataSelector customCharacterDataSelector)
        {
            _container = container;
            _charactersRepository = charactersRepository;
            _customerGuid = customerGuid;
            _customCharacterDataSelector = customCharacterDataSelector;
        }

        /// <summary>
        /// OnActionExecuting override
        /// </summary>
        /// <remarks>
        /// Checks for an empty IHeaderCustomerGUID.
        /// </remarks>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_customerGuid.CustomerGUID == Guid.Empty)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        /// <summary>
        /// Get Characters Data by Character Name.
        /// </summary>
        /// <remarks>
        /// Gets a Characters by Name.
        /// </remarks>
        [HttpPost]
        [Route("ByName")]
        [Produces(typeof(GetCharByCharName))]
        /*[SwaggerOperation("ByName")]
        [SwaggerResponse(200)]
        [SwaggerResponse(404)]*/
        public async Task<IActionResult> GetByName([FromBody] GetByNameRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid, _customCharacterDataSelector);
            return await request.Handle();
        }

    }
}
