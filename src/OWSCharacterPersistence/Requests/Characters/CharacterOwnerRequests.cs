using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSCharacterPersistence.Requests.Characters
{
    // Zone server only (service key). With a TicketGUID it claims the character the ticket names; with only a
    // CharacterName (PIE, standalone) it claims that character directly.
    public class ClaimCharacterRequest
    {
        public Guid TicketGUID { get; set; }
        public string CharacterName { get; set; }
        public int MapInstanceID { get; set; }

        private Guid customerGUID;
        private ICharacterOwnersRepository ownersRepository;

        public void SetData(ICharacterOwnersRepository ownersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.ownersRepository = ownersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            if (TicketGUID != Guid.Empty)
            {
                return new OkObjectResult(await ownersRepository.ClaimWithTicket(customerGUID, TicketGUID, MapInstanceID));
            }
            if (!String.IsNullOrEmpty(CharacterName))
            {
                return new OkObjectResult(await ownersRepository.ClaimByName(customerGUID, CharacterName, MapInstanceID));
            }
            return new OkObjectResult(new ClaimCharacterResult { Success = false, ErrorMessage = "bad_request" });
        }
    }

    public class CancelJoinTicketRequest
    {
        public Guid TicketGUID { get; set; }

        private Guid customerGUID;
        private ICharacterOwnersRepository ownersRepository;

        public void SetData(ICharacterOwnersRepository ownersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.ownersRepository = ownersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            return new OkObjectResult(await ownersRepository.CancelTicket(customerGUID, TicketGUID));
        }
    }

    public class CheckOwnersRequest
    {
        public List<CharacterOwnerCheck> Owners { get; set; }

        private Guid customerGUID;
        private ICharacterOwnersRepository ownersRepository;

        public void SetData(ICharacterOwnersRepository ownersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.ownersRepository = ownersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            return new OkObjectResult(new CheckOwnersResult
            {
                Success = true,
                ErrorMessage = "",
                Stale = await ownersRepository.GetStaleOwners(customerGUID, Owners)
            });
        }
    }
}
