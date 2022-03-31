using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Requests.Characters
{
    /// <summary>
    /// Get Custom Data
    /// </summary>
    /// <remarks>
    /// Get a list of all Custom Data fields for this character with their Field Values.  Characters can have zero or more Custom Data fields.
    /// </remarks>
    public class GetCustomDataRequest
    {
        /// <summary>
        /// Character Name
        /// </summary>
        /// <remarks>
        /// This is the name of the character to get Custom Data fields for.
        /// </remarks>
        public string CharacterName { get; set; }

        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<CustomCharacterDataRows> Handle()
        {
            CustomCharacterDataRows output = new CustomCharacterDataRows();

            output.Rows = await charactersRepository.GetCustomCharacterData(customerGUID, CharacterName);

            return output;
        }

    }
}
