using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSCharacterPersistence.Requests.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.Tables;
using OWSData.Models.Composites;

namespace OWSCharacterPersistence.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CharactersController : Controller
    {
        private readonly ICharactersRepository _charactersRepository;
        private readonly IHeaderCustomerGUID _customerGuid;

        public CharactersController(ICharactersRepository charactersRepository,
            IHeaderCustomerGUID customerGuid)
        {
            _charactersRepository = charactersRepository;
            _customerGuid = customerGuid;
        }

        [HttpPost]
        [Route("AddOrUpdateCustomData")]
        public async Task AddOrUpdateCustomData([FromBody] AddOrUpdateCustomDataRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            await request.Handle();

            return;
        }

        [HttpPost]
        [Route("GetByName")]
        [Produces(typeof(GetCharByCharName))]
        public async Task<IActionResult> GetByName([FromBody] GetByNameRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("GetCustomData")]
        [Produces(typeof(CustomCharacterDataRows))]
        public async Task<CustomCharacterDataRows> GetCustomData([FromBody] GetCustomDataRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("UpdateAllPlayerPositions")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> UpdateAllPlayerPositions([FromBody] UpdateAllPlayerPositionsRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("UpdateCharacterStats")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> UpdateCharacterStats([FromBody] UpdateCharacterStatsRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }

        [HttpPost]
        [Route("PlayerLogout")]
        [Produces(typeof(SuccessAndErrorMessage))]
        public async Task<SuccessAndErrorMessage> PlayerLogout([FromBody] PlayerLogoutRequest request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Handle();
        }
    }
}
