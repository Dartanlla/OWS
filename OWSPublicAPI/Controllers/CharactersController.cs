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

namespace TestCore2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CharactersController : Controller
    {
        private readonly Container _container;

        public CharactersController(Container container)
        {
            _container = container;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            IHeaderCustomerGUID customerGuid = _container.GetInstance<IHeaderCustomerGUID>();

            if (customerGuid.CustomerGUID == Guid.Empty)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        [HttpGet("{id}")]
        public void Get([FromQuery] int value)
        {

        }

        [HttpGet]
        [Route("ByUserGUID")]
        public void GetByUserGUID([FromQuery] string UserGuid)
        {

        }

        [HttpGet]
        [Route("ByUserEmail")]
        public void GetByUserEmail([FromQuery] string Email)
        {

        }

        [HttpPost]
        [Route("ByName")]
        [Produces(typeof(GetCharByCharName))]
        /*[SwaggerOperation("ByName")]
        [SwaggerResponse(200)]
        [SwaggerResponse(404)]*/
        public async Task<IActionResult> GetByName([FromBody] GetByNameRequest request)
        {
            return await request.Handle();
        }

        // POST api/values
        [HttpPost("{id}")]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
