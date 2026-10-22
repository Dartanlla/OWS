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
    public class UpdateCharacterStatsRequest : IOwnedCharacterWrite
    {
        public string CharacterName { get; set; }
        public Guid? OwnerToken { get; set; }
        string IOwnedCharacterWrite.OwnedCharacterName => CharacterName;
       
        public IEnumerable<UpdateCharacterStats> CharacterStats { get; set; }

        // MapInstanceID of the zone server sending this save. The backend refuses the write if the
        // character has since been handed to a different instance, so an in-flight save from the zone
        // the player just left cannot overwrite the destination's newer state. Omit for legacy callers.
        public int? ZoneInstanceID { get; set; }

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
                if (!await charactersRepository.UpdateCharacterStats(customerGUID, CharacterName, CharacterStats, ZoneInstanceID))
                {
                    // Success stays true: the caller is fire-and-forget and must not retry a write that
                    // was refused precisely because a newer zone owns the character.
                    successAndErrorMessage.ErrorMessage = "stale_zone";
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
