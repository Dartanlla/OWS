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

        private GetCharByCharName Output;
        private Guid CustomerGUID;
        private ICharactersRepository charactersRepository;

        public GetCharacterStatusesRequest(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            //CustomerGUID = new Guid("56FB0902-6FE7-4BFE-B680-E3C8E497F016");
            CustomerGUID = customerGuid.CustomerGUID;
            this.charactersRepository = charactersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            Output = await charactersRepository.GetCharByCharName(CustomerGUID, CharacterName);

            return new OkObjectResult(Output);
        }
    }
}
