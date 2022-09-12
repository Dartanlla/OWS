using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSCharacterPersistence.Requests.Characters
{
    public class GetByNameRequest
    {
        public string CharacterName { get; set; }

        private CharactersExtended output;
        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            output = await charactersRepository.GetCharacterExtendedByName(customerGUID, CharacterName);

            return new OkObjectResult(output);
        }
    }
}
