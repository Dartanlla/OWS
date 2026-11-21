using System;

namespace OWSData.Models
{
    public sealed class EdgeServerEndpointInUseException : Exception
    {
        public EdgeServerEndpointInUseException(Exception innerException)
            : base("An Edge server already uses this endpoint.", innerException)
        {
        }
    }
}
