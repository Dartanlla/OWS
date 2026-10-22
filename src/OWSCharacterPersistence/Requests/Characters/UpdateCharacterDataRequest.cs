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
    public class UpdateCharacterDataRequest : IOwnedCharacterWrite
    {
        public string CharacterName { get; set; }
        public Guid? OwnerToken { get; set; }
        string IOwnedCharacterWrite.OwnedCharacterName => CharacterName;
       
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

                // The inventory write can now fail its own guard (missing CharInventory row, and
                // stale_revision once callers opt in). It used to return void, so a refused bag save
                // reported success to the zone server; surface it instead.
                UpdateCharacterInventoryResponse inventoryResult =
                    await charactersRepository.UpdateCharacterInventory(customerGUID, CharacterName, CharInventory);

                // Deliberately NOT an early return. These three are separate transactions, so
                // bailing here would leave quests committed and silently drop the stats save
                // forever — a refused bag must not also cost the player their stats. Run the
                // remaining write, then report the inventory failure.
                await charactersRepository.UpdateCharacterStats(customerGUID, CharacterName, CharStats);

                if (!inventoryResult.Success)
                {
                    successAndErrorMessage.Success = false;
                    successAndErrorMessage.ErrorMessage = $"UpdateCharacterInventory failed: {inventoryResult.ReasonCode}";
                }
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
