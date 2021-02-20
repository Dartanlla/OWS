using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSCharacterPersistence.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : Controller
    {
        private readonly ICharactersRepository _charactersRepository;
        private readonly IHeaderCustomerGUID _customerGuid;

        public StatusController(ICharactersRepository charactersRepository,
            IHeaderCustomerGUID customerGuid)
        {
            _charactersRepository = charactersRepository;
            _customerGuid = customerGuid;
        }

        [HttpPost]
        [Route("GetCharacterStatuses")]
        [Produces(typeof(CharacterStatuses))]
        public async Task<IActionResult> CreateCharacter([FromBody] GetCharacterStatuses request)
        {
            request.SetData(_charactersRepository, _customerGuid);
            return await request.Run();
        }
    }
}