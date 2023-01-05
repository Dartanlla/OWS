using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
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
        
        public string UserSessionGUID { get; set; }
        public string CharacterName { get; set; }

        private CharacterAndCustomData Output;
        private Guid CustomerGUID;
        //private IServiceProvider ServiceProvider;
        private ICharactersRepository charactersRepository;
        private ICustomCharacterDataSelector customCharacterDataSelector;

        /// <summary>
        /// Set Dependencies for GetByNameRequest
        /// </summary>
        /// <remarks>
        /// Injects the dependencies for the GetByNameRequest.
        /// </remarks>
        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, ICustomCharacterDataSelector customCharacterDataSelector)
        {
            //CustomerGUID = new Guid("56FB0902-6FE7-4BFE-B680-E3C8E497F016");
            CustomerGUID = customerGuid.CustomerGUID;
            this.charactersRepository = charactersRepository;
            this.customCharacterDataSelector = customCharacterDataSelector;
        }

        /// <summary>
        /// Handles the GetByNameRequest
        /// </summary>
        /// <remarks>
        /// Overrides IRequestHandler Handle().
        /// </remarks>
        public async Task<IActionResult> Handle()
        {
            

            Output = new CharacterAndCustomData();

            //Get character data
            GetCharByCharName characterData = await charactersRepository.GetCharByCharName(CustomerGUID, CharacterName);
            Output.CharacterData = characterData;

            //Get custom character data
            IEnumerable<CustomCharacterData> customCharacterDataItems = await charactersRepository.GetCustomCharacterData(CustomerGUID, CharacterName);

            //Initialize the list
            Output.CustomCharacterDataRows = new List<CustomCharacterData>();

            //Loop through all the CustomCharacterData rows
            foreach (CustomCharacterData currentCustomCharacterData in customCharacterDataItems)
            {
                //Use the ICustomCharacterDataSelector implementation to filter which fields are returned
                if (customCharacterDataSelector.ShouldExportThisCustomCharacterDataField(currentCustomCharacterData.CustomFieldName))
                {
                    //Add the filtered Custom Character Data
                    Output.CustomCharacterDataRows.Add(currentCustomCharacterData);
                }
            }

            return new OkObjectResult(Output);
        }
    }
}
