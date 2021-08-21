using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Characters
{
    /// <summary>
    /// GetByNameRequest Handler
    /// </summary>
    /// <remarks>
    /// Handles api/Characters/GetByName requests.
    /// </remarks>
    public class GetByNameRequest : IRequestHandler<GetByNameRequest, IActionResult>, IRequest
    {
        /// <summary>
        /// CharacterName Request Paramater.
        /// </summary>
        /// <remarks>
        /// Contains the Character Name from the request
        /// </remarks>
        public string CharacterName { get; set; }

        private GetCharByCharName Output;
        private Guid CustomerGUID;
        //private IServiceProvider ServiceProvider;
        private ICharactersRepository charactersRepository;

        /// <summary>
        /// Set Dependencies for GetByNameRequest
        /// </summary>
        /// <remarks>
        /// Injects the dependencies for the GetByNameRequest.
        /// </remarks>
        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            //CustomerGUID = new Guid("56FB0902-6FE7-4BFE-B680-E3C8E497F016");
            CustomerGUID = customerGuid.CustomerGUID;
            this.charactersRepository = charactersRepository;
        }

        /// <summary>
        /// Handles the GetByNameRequest
        /// </summary>
        /// <remarks>
        /// Overrides IRequestHandler Handle().
        /// </remarks>
        public async Task<IActionResult> Handle()
        {
            Output = await charactersRepository.GetCharByCharName(CustomerGUID, CharacterName);

            return new OkObjectResult(Output);
        }
    }
}
