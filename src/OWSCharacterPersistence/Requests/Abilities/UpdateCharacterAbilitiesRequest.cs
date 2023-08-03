using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Requests.Abilities
{
    /// <summary>
    /// Update Ability on Character
    /// </summary>
    /// <remarks>
    /// Update the Ability on the Character to modify Ability Level and the per instance Custom JSON
    /// </remarks>
    public class UpdateCharacterAbilitiesRequest
    {
        /// <summary>
        /// Character Name
        /// </summary>
        /// <remarks>
        /// This is the name of the character to update the ability on.
        /// </remarks>
        public string CharacterName { get; set; }

        public IEnumerable<UpdateCharacterAbilities> CharacterAbilities { get; set; }
        
        private SuccessAndErrorMessage output;
        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<SuccessAndErrorMessage> Handle()
        {
            output = new SuccessAndErrorMessage();
            await charactersRepository.UpdateCharacterAbilities(customerGUID, CharacterName, CharacterAbilities);

            output.Success = true;
            output.ErrorMessage = "";

            return output;
        }
    }
}
