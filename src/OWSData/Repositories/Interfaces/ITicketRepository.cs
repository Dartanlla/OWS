using System;
using System.Threading.Tasks;
using OWSData.Models.Tickets;

namespace OWSData.Repositories.Interfaces
{
    public interface ITicketRepository
    {
        Task<bool> AddTicketAsync(ConnectionTicket ticket, TimeSpan timeToLive);

        // Read the secret to verify the encrypted handshake before claiming it.
        Task<ConnectionTicket> GetTicketAsync(string ticket, Guid customerGuid, int serverId);

        // ClaimantId must identify one authenticated Edge handshake, including its
        // client endpoint, so a retransmission can receive the same ticket record.
        Task<ConnectionTicket> RedeemTicketAsync(
            string ticket, Guid customerGuid, int serverId, string claimantId);
    }
}
