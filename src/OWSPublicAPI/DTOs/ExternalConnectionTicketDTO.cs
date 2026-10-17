using System;

namespace OWSPublicAPI.DTOs
{
    /// <summary>Requests a connection ticket for an OWS user session and Edge server.</summary>
    public class ExternalConnectionTicketRequest
    {
        /// <summary>The OWS session returned by login.</summary>
        public Guid UserSessionGUID { get; set; }
        /// <summary>The intended Nilux Edge server identifier.</summary>
        public int ServerId { get; set; }
    }

    /// <summary>Credentials used once to authenticate a Nilux Edge UDP connection.</summary>
    public class ExternalConnectionTicketResponse
    {
        /// <summary>Opaque, short-lived connection ticket.</summary>
        public string Ticket { get; set; }
        /// <summary>Fresh Base64-encoded 32-byte secret for the secure datagram handshake.</summary>
        public string SessionSecretBase64 { get; set; }
        /// <summary>The character selected in the OWS session.</summary>
        public int CharacterId { get; set; }
        /// <summary>The intended Nilux Edge server identifier.</summary>
        public int ServerId { get; set; }
        /// <summary>The UTC expiration of the ticket.</summary>
        public DateTimeOffset ExpiresAtUtc { get; set; }
    }

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
        public DateTimeOffset ExpiresAtUtc { get; set; }
    }
}
