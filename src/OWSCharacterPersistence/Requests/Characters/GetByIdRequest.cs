using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Requests.Characters
{
    /// <summary>
    /// Looks up a customer's character and all stored custom character data.
    /// </summary>
    public class GetByIdRequest
    {
        private readonly int _characterId;
        private Guid _customerGuid;
        private ICharactersRepository _charactersRepository;

        /// <summary>
        /// Creates a lookup using the character ID from the route.
        /// </summary>
        public GetByIdRequest(int characterId)
        {
            _characterId = characterId;
        }

        /// <summary>
        /// Supplies the repository and customer scope for the lookup.
        /// </summary>
        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            _charactersRepository = charactersRepository;
            _customerGuid = customerGuid.CustomerGUID;
        }

        /// <summary>
        /// Returns the character with every stored custom field for this customer.
        /// </summary>
        public async Task<IActionResult> Handle()
        {
            if (_customerGuid == Guid.Empty)
                return new UnauthorizedResult();

            var output = new CharacterAndCustomData();
            if (_characterId <= 0)
                return new BadRequestObjectResult(output);

            var character = await _charactersRepository.GetCharByCharId(_customerGuid, _characterId);
            if (character == null)
                return new BadRequestObjectResult(output);

            output.CharacterData = character;
            var customData = await _charactersRepository.GetCustomCharacterData(_customerGuid, character.CharName);
            output.CustomCharacterDataRows = customData.Select(row => new CustomCharacterDataDTO
            {
                CustomFieldName = row.CustomFieldName,
                FieldValue = row.FieldValue
            }).ToList();

            return new OkObjectResult(output);
        }
    }
}
