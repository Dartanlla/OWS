using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSCharacterPersistence.Requests.Characters
{
    /// <summary>
    /// GetByNameRequest Handler
    /// </summary>
    /// <remarks>
    /// Handles api/Characters/GetByName requests.
    /// </remarks>
    public class GetQuestsFromDatabaseRequest
    {
        //public string CharacterName { get; set; }

        private IEnumerable<GetQuestsFromDb> Outputs { get; set; }
        private Guid _customerGUID { get; set; }
        private ICharactersRepository _charactersRepository { get; set; }

        /// <summary>
        /// Constructor for GetByNameRequest
        /// </summary>
        /// <remarks>
        /// Injects the dependencies for the GetByNameRequest.
        /// </remarks>
        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            _customerGUID = customerGuid.CustomerGUID;
            _charactersRepository = charactersRepository;
        }

        /// <summary>
        /// Handles the GetByNameRequest
        /// </summary>
        /// <remarks>
        /// Overrides IRequestHandler Handle().
        /// </remarks>
        public async Task<IActionResult> Handle()
        {
            Outputs = await _charactersRepository.GetQuestsFromDatabase(_customerGUID);
            return new OkObjectResult(Outputs);
        }
    }
}
