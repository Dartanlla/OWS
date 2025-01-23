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
    public class GetDefaultCustomDataRequest : IRequestHandler<GetDefaultCustomDataRequest, IActionResult>, IRequest
    {
        private readonly GetDefaultCustomrDataDTO _getDefaultCustomCharacterDataDTO;
        private readonly Guid _customerGUID;
        private readonly IUsersRepository _usersRepository;
        private readonly ICharactersRepository _charactersRepository;
        private readonly ICustomDataSelector _customDataSelector;
        private readonly IGetReadOnlyPublicCharacterData _getReadOnlyPublicCharacterData;

        /// <summary>
        /// Constructor for GetDefaultCustomerCharacterDataRequest
        /// </summary>
        /// <remarks>
        /// Injects the dependencies for the GetDefaultCustomerCharacterDataRequest.
        /// </remarks>
        public GetDefaultCustomDataRequest(GetDefaultCustomrDataDTO getDefaultCustomCharacterDataDTO, IUsersRepository usersRepository, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, 
            ICustomDataSelector customDataSelector, IGetReadOnlyPublicCharacterData getReadOnlyPublicCharacterData)
        {
            _getDefaultCustomCharacterDataDTO = getDefaultCustomCharacterDataDTO;
            _customerGUID = customerGuid.CustomerGUID;
            _usersRepository = usersRepository;
            _charactersRepository = charactersRepository;
            _customDataSelector = customDataSelector;
            _getReadOnlyPublicCharacterData = getReadOnlyPublicCharacterData;
        }

        /// <summary>
        /// Handles the GetDefaultCustomerCharacterDataRequest
        /// </summary>
        /// <remarks>
        /// Overrides IRequestHandler Handle().
        /// </remarks>
        public async Task<IActionResult> Handle()
        {
            DefaultCustomDataRows Output = new DefaultCustomDataRows();

            //Test if a valid Guid was passed
            if (!Guid.TryParse(_getDefaultCustomCharacterDataDTO.UserSessionGUID, out Guid parsedGuid))
            {
                return new BadRequestObjectResult(Output);
            }

            //Get the User Session
            GetUserSession userSession = await _usersRepository.GetUserSession(_customerGUID, parsedGuid);

            //Make sure the User Session is valid
            if (userSession == null || !userSession.UserGuid.HasValue)
            {
                return new BadRequestObjectResult(Output);
            }

            IEnumerable<DefaultCustomData> customDataItems = await _charactersRepository.GetDefaultCustomCharacterData(_customerGUID, _getDefaultCustomCharacterDataDTO.DefaultSetName);
            Output.Rows = new List<DefaultCustomData>();
            //Loop through all the CustomCharacterData rows

            foreach (DefaultCustomData currentCustomData in customDataItems)
            {
                //Use the ICustomCharacterDataSelector implementation to filter which fields are returned
                if (_customDataSelector.ShouldExportThisCustomDataField(currentCustomData.CustomFieldName))
                {
                    DefaultCustomData customDataDTO = new DefaultCustomData()
                    {
                        CustomFieldName = currentCustomData.CustomFieldName,
                        FieldValue = currentCustomData.FieldValue
                    };
                    //Add the filtered Custom Character Data
                    Output.Rows.Add(customDataDTO);
                }
            }

            // Output.Rows = await _charactersRepository.GetDefaultCustomCharacterData(_customerGUID, _getDefaultCustomCharacterDataDTO.DefaultSetName);


            return new OkObjectResult(Output);
        }
    }
}
