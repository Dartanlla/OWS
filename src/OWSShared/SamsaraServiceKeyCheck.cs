using System;

namespace OWSShared
{
    /// <summary>
    /// The zone servers' server-to-server key (X-Samsara-Service-Key), read from a server-only env var and never present
    /// in client configs. Fail closed: outside the Development environment, an unset key rejects every caller rather
    /// than opening the gate, so a missing env var on a deploy surfaces as refusals.
    /// </summary>
    public static class SamsaraServiceKeyCheck
    {
        public const string Header = "X-Samsara-Service-Key";
        private static readonly string Configured = Environment.GetEnvironmentVariable("OWS_SAMSARA_SERVICE_KEY");
        private static readonly bool IsDevelopmentEnvironment = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

        public static bool IsValid(string provided)
        {
            if (string.IsNullOrEmpty(Configured)) return IsDevelopmentEnvironment;
            return !string.IsNullOrEmpty(provided)
                   && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                       System.Text.Encoding.UTF8.GetBytes(provided),
                       System.Text.Encoding.UTF8.GetBytes(Configured));
        }
    }
}
