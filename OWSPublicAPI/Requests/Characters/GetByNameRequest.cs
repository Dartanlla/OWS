using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Characters
{
    public class GetByNameRequest : IRequestHandler<GetByNameRequest, IActionResult>, IRequest
    {        
        public string CharacterName { get; set; }

        private GetCharByCharName Output;
        private Guid CustomerGUID;
        //private IServiceProvider ServiceProvider;
        private ICharactersRepository charactersRepository;

        public GetByNameRequest(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
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
