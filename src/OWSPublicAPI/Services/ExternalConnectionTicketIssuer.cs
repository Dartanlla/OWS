using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using OWSData.Models.Tickets;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.DTOs;

namespace OWSPublicAPI.Services
{
    public enum TicketIssueStatus { Issued, InvalidRequest, InvalidSession, AllocationFailed }

    public sealed class TicketIssueResult
    {
        public TicketIssueStatus Status { get; init; }
        public ExternalConnectionTicketResponse Response { get; init; }
    }

    /// <summary>Shared ticket issuance rules for the ASP.NET API and AWS Lambda wrapper.</summary>
    public sealed class ExternalConnectionTicketIssuer
    {
        private readonly IUsersRepository _usersRepository;
        private readonly ITicketRepository _ticketRepository;

        public ExternalConnectionTicketIssuer(IUsersRepository usersRepository, ITicketRepository ticketRepository)
        {
            _usersRepository = usersRepository;
            _ticketRepository = ticketRepository;
        }

        public async Task<TicketIssueResult> IssueAsync(Guid customerGuid, ExternalConnectionTicketRequest request)
        {
            if (request == null || request.UserSessionGUID == Guid.Empty || request.ServerId <= 0)
                return new TicketIssueResult { Status = TicketIssueStatus.InvalidRequest };

            if (customerGuid == Guid.Empty)
                return new TicketIssueResult { Status = TicketIssueStatus.InvalidSession };

            var session = await _usersRepository.GetUserSession(customerGuid, request.UserSessionGUID);
            if (session == null || session.UserSessionGUID != request.UserSessionGUID
                || session.CustomerGuid != customerGuid
                || !session.UserGuid.HasValue || session.UserGuid.Value == Guid.Empty
                || session.CharacterID <= 0
                || string.IsNullOrWhiteSpace(session.SelectedCharacterName)
                || !string.Equals(session.SelectedCharacterName, session.CharName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new TicketIssueResult { Status = TicketIssueStatus.InvalidSession };
            }

            var expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(45);
            var ticket = new ConnectionTicket
            {
                SessionSecretBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                CustomerGuid = session.CustomerGuid,
                UserGuid = session.UserGuid.Value,
                UserSessionGuid = session.UserSessionGUID,
                CharacterId = session.CharacterID,
                ServerId = request.ServerId,
                ExpiresAtUtc = expiresAtUtc
            };

            for (var attempt = 0; attempt < 3; attempt++)
            {
                ticket.Ticket = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                    .TrimEnd('=').Replace('+', '-').Replace('/', '_');
                if (await _ticketRepository.AddTicketAsync(ticket, expiresAtUtc - DateTimeOffset.UtcNow))
                {
                    return new TicketIssueResult
                    {
                        Status = TicketIssueStatus.Issued,
                        Response = new ExternalConnectionTicketResponse
                        {
                            Ticket = ticket.Ticket,
                            SessionSecretBase64 = ticket.SessionSecretBase64,
                            CharacterId = ticket.CharacterId,
                            ServerId = ticket.ServerId,
                            ExpiresAtUtc = ticket.ExpiresAtUtc
                        }
                    };
                }
            }

            return new TicketIssueResult { Status = TicketIssueStatus.AllocationFailed };
        }
    }
}
