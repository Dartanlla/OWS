using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Requests.Abilities
{
    /// <summary>
    /// Remove Ability from Character
    /// </summary>
    /// <remarks>
    /// Removes an Ability from a Character
    /// </remarks>
    public class RemoveAbilityFromCharacterRequest
    {
        /// <summary>
        /// Ability Name
        /// </summary>
        /// <remarks>
        /// This is the name of the ability to add to the character.
        /// </remarks>
        public string AbilityName { get; set; }
        /// <summary>
        /// Character Name
        /// </summary>
        /// <remarks>
        /// This is the name of the character to add the ability to.
        /// </remarks>
        public string CharacterName { get; set; }

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
            await charactersRepository.RemoveAbilityFromCharacter(customerGUID, AbilityName, CharacterName);

            output.Success = true;
            output.ErrorMessage = "";

            return output;
        }
    }
}
