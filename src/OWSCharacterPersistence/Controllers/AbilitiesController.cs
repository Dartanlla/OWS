using Microsoft.AspNetCore.Mvc;
using OWSCharacterPersistence.Requests.Abilities;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AbilitiesController : Controller
    {
        private readonly ICharactersRepository _charactersRepository;
        private readonly IHeaderCustomerGUID _customerGuid;

        public AbilitiesController(ICharactersRepository charactersRepository,
            IHeaderCustomerGUID customerGuid)
        {
            _charactersRepository = charactersRepository;
            _customerGuid = customerGuid;
        }

        /*
        [HttpPost]
        [Route("GetAbilities")]
        [Produces(typeof(GetAbilities))]
        public async Task<IActionResult> GetAbilities([FromBody] GetAbilitiesRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }
        */

        [HttpPost]
        [Route("GetCharacterAbilities")]
        [Produces(typeof(IEnumerable<GetCharacterAbilities>))]
        public async Task<IActionResult> GetCharacterAbilities([FromBody] GetCharacterAbilitiesRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("GetAbilityBars")]
        [Produces(typeof(IEnumerable<GetAbilityBars>))]
        public async Task<IActionResult> GetAbilityBars([FromBody] GetAbilityBarsRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("GetAbilityBarsAndAbilities")]
        [Produces(typeof(IEnumerable<GetAbilityBarsAndAbilities>))]
        public async Task<IActionResult> GetAbilityBarsAndAbilities([FromBody] GetCharacterAbilitiesRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }
    }
}
