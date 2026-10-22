using System;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSData.Repositories.Interfaces
{
    /// <summary>
    /// Single-use join tickets (Postgres only): GetServerToConnectTo issues one, the zone server redeems it once.
    /// </summary>
    public interface IJoinTicketsRepository
    {
        Task<Guid> IssueTicket(Guid customerGUID, Guid userSessionGUID, Guid userGUID, int characterID, string characterName,
            int mapInstanceID, JoinTicketStart start);
        Task<RedeemJoinTicketResult> RedeemTicket(Guid customerGUID, Guid ticketGUID, int mapInstanceID);
    }
}
