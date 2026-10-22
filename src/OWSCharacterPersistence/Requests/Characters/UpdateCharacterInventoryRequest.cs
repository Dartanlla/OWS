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
    public class UpdateCharacterInventoryRequest : IOwnedCharacterWrite
    {
        public string CharacterName { get; set; }
        public Guid? OwnerToken { get; set; }
        string IOwnedCharacterWrite.OwnedCharacterName => CharacterName;

        public IEnumerable<UpdateCharacterInventory> Inventory { get; set; }

        // Opt-in optimistic lock. Send the Characters.EconomyRevision this bag was computed from and
        // a snapshot that lost a race with a shop transaction is rejected with "stale_revision"
        // instead of overwriting it; resync from NewEconomyRevision, recompute the bag, resend.
        // Omit (or send <= 0) to keep the legacy last-write-wins behaviour.
        public long ExpectedRevision { get; set; }

        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<UpdateCharacterInventoryResponse> Handle()
        {
            try
            {
                return await charactersRepository.UpdateCharacterInventory(customerGUID, CharacterName, Inventory,
                    ExpectedRevision > 0 ? ExpectedRevision : (long?)null);
            }
            catch (Exception ex)
            {
                return new UpdateCharacterInventoryResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ReasonCode = "exception"
                };
            }
        }
    }
}
