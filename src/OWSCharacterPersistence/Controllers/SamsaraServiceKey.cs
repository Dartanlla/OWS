using System;
using Microsoft.AspNetCore.Http;

namespace OWSCharacterPersistence.Controllers
{
    /// <summary>
    /// The zone servers' server-to-server key (X-Samsara-Service-Key), read from a server-only env var and never present
    /// in client configs. Fail closed: outside the Development environment, an unset key rejects every request rather than
    /// opening the gate, so a missing env var on a deploy surfaces as 401s.
    /// </summary>
    internal static class SamsaraServiceKey
    {
        private const string Header = "X-Samsara-Service-Key";
        private static readonly string Configured = Environment.GetEnvironmentVariable("OWS_SAMSARA_SERVICE_KEY");
        private static readonly bool IsDevelopmentEnvironment = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

        public static bool IsValid(HttpRequest request)
        {
            if (string.IsNullOrEmpty(Configured)) return IsDevelopmentEnvironment;
            return request.Headers.TryGetValue(Header, out var provided)
                   && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                       System.Text.Encoding.UTF8.GetBytes(provided.ToString()),
                       System.Text.Encoding.UTF8.GetBytes(Configured));
        }
    }
}
