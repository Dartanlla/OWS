using System;
using System.Threading.Tasks;
using OWSData.Models.Tickets;
using OWSData.Repositories.Interfaces;

namespace OWSInstanceManagement.Services
{
    public static class ExternalConnectionTicketRedemption
    {
        public static bool IsValid(RedeemExternalConnectionTicketRequest request) =>
            request != null
            && !string.IsNullOrWhiteSpace(request.Ticket)
            && request.ServerId > 0
            && (!request.Claim || !string.IsNullOrWhiteSpace(request.ClaimantId));

        public static Task<ConnectionTicket> ExecuteAsync(
            ITicketRepository repository,
            RedeemExternalConnectionTicketRequest request, Guid tenant) =>
            request.Claim
                ? repository.RedeemTicketAsync(
                    request.Ticket, tenant, request.ServerId, request.ClaimantId)
                : repository.GetTicketAsync(request.Ticket, tenant, request.ServerId);

        public static RedeemExternalConnectionTicketResponse ToResponse(ConnectionTicket ticket) =>
            new RedeemExternalConnectionTicketResponse
            {
                SessionSecretBase64 = ticket.SessionSecretBase64,
                CustomerGuid = ticket.CustomerGuid,
                UserGuid = ticket.UserGuid,
                UserSessionGuid = ticket.UserSessionGuid,
                CharacterId = ticket.CharacterId,
                ServerId = ticket.ServerId,
                MapInstanceId = ticket.MapInstanceId,
                ExpiresAtUtc = ticket.ExpiresAtUtc
            };
    }
}
