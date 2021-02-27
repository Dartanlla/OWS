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

        /// <summary>
        /// Constructor for Public Character related API calls.
        /// </summary>
        /// <remarks>
        /// All dependencies are injected.
        /// </remarks>
        public CharactersController(Container container, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            _container = container;
            _charactersRepository = charactersRepository;
            _customerGuid = customerGuid;
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
        /// Get Character Data by CharacterID.
        /// </summary>
        /// <remarks>
        /// Gets all character data by Character ID.
        /// </remarks>
        [HttpGet("{id}")]
        public void Get([FromQuery] int value)
        {

        }

        /// <summary>
        /// Get Characters Data by UserGUID.
        /// </summary>
        /// <remarks>
        /// Gets a list of Characters by UserGUID.
        /// </remarks>
        [HttpGet]
        [Route("ByUserGUID")]
        public void GetByUserGUID([FromQuery] string UserGuid)
        {

        }

        /// <summary>
        /// Get Characters Data by Email.
        /// </summary>
        /// <remarks>
        /// Gets a list of Characters by Email.
        /// </remarks>
        [HttpGet]
        [Route("ByUserEmail")]
        public void GetByUserEmail([FromQuery] string Email)
        {

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
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }


        /// <summary>
        /// Delete a Character Permanently.
        /// </summary>
        /// <remarks>
        /// Deletes a character by CharacterID.
        /// </remarks>
        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
