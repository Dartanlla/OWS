using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Requests.Characters
{
    public class RedeemJoinTicketRequest
    {
        public Guid TicketGUID { get; set; }
        // The redeeming zone server's own instance: a ticket issued for another instance is refused.
        public int MapInstanceID { get; set; }

        private Guid customerGUID;
        private IJoinTicketsRepository joinTicketsRepository;

        public void SetData(IJoinTicketsRepository joinTicketsRepository, IHeaderCustomerGUID customerGuid)
        {
            this.joinTicketsRepository = joinTicketsRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            return new OkObjectResult(await joinTicketsRepository.RedeemTicket(customerGUID, TicketGUID, MapInstanceID));
        }
    }
}
