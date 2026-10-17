using System;

namespace OWSData.Models.Tickets
{
    public class ConnectionTicket
    {
        public string Ticket { get; set; }
        public string SessionSecretBase64 { get; set; }
        public Guid CustomerGuid { get; set; }
        public Guid UserGuid { get; set; }
        public Guid UserSessionGuid { get; set; }
        public int CharacterId { get; set; }
        public int ServerId { get; set; }
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public string RedeemedBy { get; set; }
    }
}
