using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSCharacterPersistence.Requests.Statuses
{
    public class GetCharacterStatusesRequest : IRequestHandler<GetCharacterStatusesRequest, IActionResult>, IRequest
    {
        public string CharacterName { get; set; }

        private GetCharByCharName output;
        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            output = await charactersRepository.GetCharByCharName(customerGUID, CharacterName);

            return new OkObjectResult(output);
        }
    }
}
