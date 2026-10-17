using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OWSData.Models.Tickets;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.DTOs;

namespace OWSPublicAPI.Services
{
    public static class ExternalConnectionTicketRedemption
    {
        public const string ApiKeyHeader = "X-Edge-Redemption-Key";
        public const string ApiKeyEnvironmentVariable = "OWS_TICKET_REDEMPTION_API_KEY";
        public const string SectionApiKeyEnvironmentVariable = "OWSTicketConfig__RedemptionApiKey";

        public static string GetConfiguredApiKey() =>
            Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable)
            ?? Environment.GetEnvironmentVariable(SectionApiKeyEnvironmentVariable);

        public static bool IsAuthorized(string providedApiKey)
        {
            string configuredApiKey = GetConfiguredApiKey();
            if (string.IsNullOrWhiteSpace(configuredApiKey)
                || string.IsNullOrWhiteSpace(providedApiKey))
            {
                return false;
            }

            byte[] expected = Encoding.UTF8.GetBytes(configuredApiKey);
            byte[] provided = Encoding.UTF8.GetBytes(providedApiKey);
            bool matches = expected.Length == provided.Length
                && CryptographicOperations.FixedTimeEquals(expected, provided);
            CryptographicOperations.ZeroMemory(expected);
            CryptographicOperations.ZeroMemory(provided);
            return matches;
        }

        public static bool IsValid(RedeemExternalConnectionTicketRequest request) =>
            request != null
            && !string.IsNullOrWhiteSpace(request.Ticket)
            && request.ServerId > 0
            && (!request.Claim || !string.IsNullOrWhiteSpace(request.ClaimantId));

        public static Task<ConnectionTicket> ExecuteAsync(
            ITicketRepository repository,
            RedeemExternalConnectionTicketRequest request) =>
            request.Claim
                ? repository.RedeemTicketAsync(
                    request.Ticket, request.ServerId, request.ClaimantId)
                : repository.GetTicketAsync(request.Ticket, request.ServerId);

        public static RedeemExternalConnectionTicketResponse ToResponse(ConnectionTicket ticket) =>
            new RedeemExternalConnectionTicketResponse
            {
                SessionSecretBase64 = ticket.SessionSecretBase64,
                CustomerGuid = ticket.CustomerGuid,
                UserGuid = ticket.UserGuid,
                UserSessionGuid = ticket.UserSessionGuid,
                CharacterId = ticket.CharacterId,
                ServerId = ticket.ServerId,
                ExpiresAtUtc = ticket.ExpiresAtUtc
            };
    }
}
