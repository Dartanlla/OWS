using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.DTOs;
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
        private readonly GetByNameDTO _getByNameDTO;
        private readonly Guid _customerGUID;
        private readonly IUsersRepository _usersRepository;
        private readonly ICharactersRepository _charactersRepository;
        private readonly ICustomCharacterDataSelector _customCharacterDataSelector;
        private readonly IGetReadOnlyPublicCharacterData _getReadOnlyPublicCharacterData;

        /// <summary>
        /// Constructor for GetByNameRequest
        /// </summary>
        /// <remarks>
        /// Injects the dependencies for the GetByNameRequest.
        /// </remarks>
        public GetByNameRequest(GetByNameDTO getByNameDTO, IUsersRepository usersRepository, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, 
            ICustomCharacterDataSelector customCharacterDataSelector, IGetReadOnlyPublicCharacterData getReadOnlyPublicCharacterData)
        {
            _getByNameDTO = getByNameDTO;
            _customerGUID = customerGuid.CustomerGUID;
            _usersRepository = usersRepository;
            _charactersRepository = charactersRepository;
            _customCharacterDataSelector = customCharacterDataSelector;
            _getReadOnlyPublicCharacterData = getReadOnlyPublicCharacterData;
        }

        /// <summary>
        /// Handles the GetByNameRequest
        /// </summary>
        /// <remarks>
        /// Overrides IRequestHandler Handle().
        /// </remarks>
        public async Task<IActionResult> Handle()
        {
            CharacterAndCustomData Output = new CharacterAndCustomData();

            //Get the User Session
            GetUserSession userSession = await _usersRepository.GetUserSession(_customerGUID, new Guid(_getByNameDTO.UserSessionGUID));

            //Make sure the User Session is valid
            if (userSession == null || !userSession.UserGuid.HasValue)
            {
                return new BadRequestObjectResult(Output);
            }

            //Get character data
            GetCharByCharName characterData = await _charactersRepository.GetCharByCharName(_customerGUID, _getByNameDTO.CharacterName);

            //Make sure the character data is valid and in the right User Session
            if (characterData == null || !characterData.UserGuid.HasValue || characterData.UserGuid != userSession.UserGuid)
            {
                return new BadRequestObjectResult(Output);
            }

            //Assign the character data to the output object
            Output.CharacterData = characterData;

            //Get custom character data
            IEnumerable<CustomCharacterData> customCharacterDataItems = await _charactersRepository.GetCustomCharacterData(_customerGUID, _getByNameDTO.CharacterName);

            //Initialize the list
            Output.CustomCharacterDataRows = new List<CustomCharacterDataDTO>();

            //Loop through all the CustomCharacterData rows
            foreach (CustomCharacterData currentCustomCharacterData in customCharacterDataItems)
            {
                //Use the ICustomCharacterDataSelector implementation to filter which fields are returned
                if (_customCharacterDataSelector.ShouldExportThisCustomCharacterDataField(currentCustomCharacterData.CustomFieldName))
                {
                    CustomCharacterDataDTO customCharacterDataDTO = new CustomCharacterDataDTO()
                    {
                        CustomFieldName = currentCustomCharacterData.CustomFieldName,
                        FieldValue = currentCustomCharacterData.FieldValue
                    };

                    //Add the filtered Custom Character Data
                    Output.CustomCharacterDataRows.Add(customCharacterDataDTO);
                }
            }

            //Add Read-only Character Data
            CustomCharacterDataDTO readOnlyCharacterData = new CustomCharacterDataDTO()
            {
                CustomFieldName = "ReadOnlyCharacterData",
                FieldValue = await _getReadOnlyPublicCharacterData.GetReadOnlyPublicCharacterData(characterData.CharacterId)
            };
            Output.CustomCharacterDataRows.Add(readOnlyCharacterData);

            return new OkObjectResult(Output);
        }
    }
}
