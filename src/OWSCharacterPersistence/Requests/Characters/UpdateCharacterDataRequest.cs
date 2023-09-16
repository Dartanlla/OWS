using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Requests.Characters
{
    public class UpdateCharacterDataRequest
    {
        public string CharacterName { get; set; }
       
        public IEnumerable<UpdateCharacterQuest> CharQuests { get; set; }

        public IEnumerable<UpdateCharacterInventory> CharInventory { get; set; }

        public IEnumerable<UpdateCharacterStats> CharStats { get; set; }

        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<SuccessAndErrorMessage> Handle()
        {
            SuccessAndErrorMessage successAndErrorMessage = new SuccessAndErrorMessage();
            successAndErrorMessage.Success = true;

            try
            {
                await charactersRepository.UpdateCharacterQuests(customerGUID, CharacterName, CharQuests);
                await charactersRepository.UpdateCharacterInventory(customerGUID, CharacterName, CharInventory);
                await charactersRepository.UpdateCharacterStats(customerGUID, CharacterName, CharStats);
            }
            catch (Exception ex)
            {
                successAndErrorMessage.ErrorMessage = ex.Message;
                successAndErrorMessage.Success = false;
            }

            return successAndErrorMessage;
        }
    }
}
