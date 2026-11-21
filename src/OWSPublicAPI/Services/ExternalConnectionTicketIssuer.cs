using OWSShared.Options;
using OWSPublicAPI.Requests.Users;
using System;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using OWSData.Models.Tickets;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.DTOs;

namespace OWSPublicAPI.Services
{
    public enum TicketIssueStatus { Issued, InvalidRequest, InvalidSession, IneligibleServer, AllocationFailed }

    public sealed class TicketIssueResult
    {
        public TicketIssueStatus Status { get; init; }
        public ExternalConnectionTicketResponse Response { get; init; }
    }

    /// <summary>Shared ticket issuance rules for the ASP.NET API and AWS Lambda wrapper.</summary>
    public sealed class ExternalConnectionTicketIssuer
    {
        private readonly IUsersRepository _usersRepository;
        private readonly IHttpClientFactory _clients;
        private readonly ITicketRepository _ticketRepository;

        public ExternalConnectionTicketIssuer(IUsersRepository usersRepository, IHttpClientFactory clients, ITicketRepository ticketRepository)
        {
            _usersRepository = usersRepository;
            _clients = clients;
            _ticketRepository = ticketRepository;
        }

        public async Task<TicketIssueResult> IssueAsync(Guid customerGuid, ExternalConnectionTicketRequest request)
        {
            if (request == null || request.UserSessionGUID == Guid.Empty || request.ServerId <= 0 || request.MapInstanceId <= 0)
                return new TicketIssueResult { Status = TicketIssueStatus.InvalidRequest };

            if (customerGuid == Guid.Empty)
                return new TicketIssueResult { Status = TicketIssueStatus.InvalidSession };

            var session = await PlayerAuthorization.GetSessionAsync(_usersRepository, customerGuid, request.UserSessionGUID);
            if (session == null
                || session.CharacterID <= 0
                || string.IsNullOrWhiteSpace(session.SelectedCharacterName)
                || !string.Equals(session.SelectedCharacterName, session.CharName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new TicketIssueResult { Status = TicketIssueStatus.InvalidSession };
            }

            using var client = _clients.CreateClient("OWSInstanceManagement");
            using var assignment = new HttpRequestMessage(HttpMethod.Post, "api/Instance/CheckReadyAssignment")
            {
                Content = JsonContent.Create(new MapInstanceOperation
                {
                    CustomerGuid = customerGuid,
                    EdgeServerId = request.ServerId,
                    MapInstanceId = request.MapInstanceId,
                    CharacterId = session.CharacterID,
                    UserGuid = session.UserGuid.Value
                })
            };
            var serviceCredential = Environment.GetEnvironmentVariable("OWS_INSTANCE_ASSIGNMENT_CREDENTIAL");
            if (string.IsNullOrWhiteSpace(serviceCredential))
                return new TicketIssueResult { Status = TicketIssueStatus.AllocationFailed };

            assignment.Headers.Add(ServerCredentials.AssignmentHeader, serviceCredential);
            using var response = await client.SendAsync(assignment);
            if (!response.IsSuccessStatusCode)
                return new TicketIssueResult { Status = TicketIssueStatus.AllocationFailed };
            if (!await response.Content.ReadFromJsonAsync<bool>())
                return new TicketIssueResult { Status = TicketIssueStatus.IneligibleServer };

            var expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(45);
            var ticket = new ConnectionTicket
            {
                SessionSecretBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                CustomerGuid = session.CustomerGuid,
                UserGuid = session.UserGuid.Value,
                UserSessionGuid = session.UserSessionGUID,
                CharacterId = session.CharacterID,
                ServerId = request.ServerId,
                MapInstanceId = request.MapInstanceId,
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
                            MapInstanceId = ticket.MapInstanceId,
                            ExpiresAtUtc = ticket.ExpiresAtUtc
                        }
                    };
                }
            }

            return new TicketIssueResult { Status = TicketIssueStatus.AllocationFailed };
        }
    }
}
