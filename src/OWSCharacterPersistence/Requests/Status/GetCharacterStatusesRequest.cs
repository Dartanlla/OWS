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
    /// <summary>
    /// GetCharacterStatusesRequest
    /// </summary>
    /// <remarks>
    /// This request object handles requests for api/Characters/GetCharacterStatuses
    /// </remarks>
    public class GetCharacterStatusesRequest : IRequestHandler<GetCharacterStatusesRequest, IActionResult>, IRequest
    {
        /// <summary>
        /// CharacterName
        /// </summary>
        /// <remarks>
        /// The CharacterName to get statses for.
        /// </remarks>
        public string CharacterName { get; set; }

        private GetCharByCharName output;
        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        /// <summary>
        /// SetData
        /// </summary>
        /// <remarks>
        /// This handles the Request.
        /// </remarks>
        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        /// <summary>
        /// Handle
        /// </summary>
        /// <remarks>
        /// This handles the Request.
        /// </remarks>
        public async Task<IActionResult> Handle()
        {
            output = await charactersRepository.GetCharByCharName(customerGUID, CharacterName);

            return new OkObjectResult(output);
        }
    }
}
