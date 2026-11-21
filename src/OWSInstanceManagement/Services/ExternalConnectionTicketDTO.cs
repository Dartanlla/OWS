using System;

namespace OWSInstanceManagement.Services
{
    /// <summary>
    /// Private Edge request for inspecting or atomically claiming an issued ticket.
    /// </summary>
    public class RedeemExternalConnectionTicketRequest
    {
        public string Ticket { get; set; }
        public int ServerId { get; set; }
        public string ClaimantId { get; set; }
        /// <summary>
        /// False retrieves the secret for handshake authentication without consuming
        /// the ticket. True atomically claims it after authentication succeeds.
        /// </summary>
        public bool Claim { get; set; }
    }

    /// <summary>Ticket identity returned only to an authenticated Edge service.</summary>
    public class RedeemExternalConnectionTicketResponse
    {
        public string SessionSecretBase64 { get; set; }
        public Guid CustomerGuid { get; set; }
        public Guid UserGuid { get; set; }
        public Guid UserSessionGuid { get; set; }
        public int CharacterId { get; set; }
        public int ServerId { get; set; }
        public int MapInstanceId { get; set; }
        public DateTimeOffset ExpiresAtUtc { get; set; }
    }
}
