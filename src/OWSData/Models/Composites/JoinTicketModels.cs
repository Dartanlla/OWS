using System;

namespace OWSData.Models.Composites
{
    // Where a redeemed join puts the character (JoinTickets.StartMode).
    public enum JoinTicketStartMode : short
    {
        StoredPosition = 0,
        Explicit = 1,
        MapDefault = 2
    }

    public class JoinTicketStart
    {
        public JoinTicketStartMode Mode { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double RX { get; set; }
        public double RY { get; set; }
        public double RZ { get; set; }
    }

    // RedeemJoinTicket reply. RX, RY, RZ are roll, pitch and yaw, as in Characters.
    public class RedeemJoinTicketResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string CharacterName { get; set; }
        public Guid UserSessionGUID { get; set; }
        public Guid UserGUID { get; set; }
        public bool UseMapDefaultStart { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double RX { get; set; }
        public double RY { get; set; }
        public double RZ { get; set; }
    }
}
