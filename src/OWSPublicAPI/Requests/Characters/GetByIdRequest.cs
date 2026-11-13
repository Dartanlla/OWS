using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.DTOs;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Characters
{
    /// <summary>
    /// Handles api/Characters/{CharacterId} requests.
    /// </summary>
    public class GetByIdRequest : IRequestHandler<GetByIdRequest, IActionResult>, IRequest
    {
        private readonly int _characterId;
        private readonly GetByIdDTO _request;
        private readonly Guid _customerGUID;
        private readonly IUsersRepository _usersRepository;
        private readonly ICharactersRepository _charactersRepository;
        private readonly ICustomCharacterDataSelector _customCharacterDataSelector;
        private readonly IGetReadOnlyPublicCharacterData _getReadOnlyPublicCharacterData;

        /// <summary>
        /// Creates a lookup using the route ID and the caller's user session.
        /// </summary>
        public GetByIdRequest(int characterId, GetByIdDTO request, IUsersRepository usersRepository,
            ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid,
            ICustomCharacterDataSelector customCharacterDataSelector,
            IGetReadOnlyPublicCharacterData getReadOnlyPublicCharacterData)
        {
            _characterId = characterId;
            _request = request;
            _customerGUID = customerGuid.CustomerGUID;
            _usersRepository = usersRepository;
            _charactersRepository = charactersRepository;
            _customCharacterDataSelector = customCharacterDataSelector;
            _getReadOnlyPublicCharacterData = getReadOnlyPublicCharacterData;
        }

        /// <summary>
        /// Returns the character with filtered custom data after checking session ownership.
        /// </summary>
        public async Task<IActionResult> Handle()
        {
            var output = new CharacterAndCustomData();
            if (_customerGUID == Guid.Empty)
                return new UnauthorizedResult();

            if (_characterId <= 0 || !Guid.TryParse(_request?.UserSessionGUID, out var sessionGuid))
                return new BadRequestObjectResult(output);

            var userSession = await _usersRepository.GetUserSession(_customerGUID, sessionGuid);
            if (userSession == null || !userSession.UserGuid.HasValue)
                return new BadRequestObjectResult(output);

            var characterData = await _charactersRepository.GetCharByCharId(_customerGUID, _characterId);
            if (characterData == null || !characterData.UserGuid.HasValue || characterData.UserGuid != userSession.UserGuid)
                return new BadRequestObjectResult(output);

            output.CharacterData = characterData;
            output.CustomCharacterDataRows = new List<CustomCharacterDataDTO>();
            var customData = await _charactersRepository.GetCustomCharacterData(_customerGUID, characterData.CharName);
            foreach (var item in customData)
            {
                if (_customCharacterDataSelector.ShouldExportThisCustomCharacterDataField(item.CustomFieldName))
                {
                    output.CustomCharacterDataRows.Add(new CustomCharacterDataDTO
                    {
                        CustomFieldName = item.CustomFieldName,
                        FieldValue = item.FieldValue
                    });
                }
            }

            output.CustomCharacterDataRows.Add(new CustomCharacterDataDTO
            {
                CustomFieldName = "ReadOnlyCharacterData",
                FieldValue = await _getReadOnlyPublicCharacterData.GetReadOnlyPublicCharacterData(characterData.CharacterId)
            });
            return new OkObjectResult(output);
        }
    }
}
