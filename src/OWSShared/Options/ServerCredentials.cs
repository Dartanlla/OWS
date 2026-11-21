using System;
using System.Security.Cryptography;
using System.Text;

namespace OWSShared.Options
{
    public static class ServerCredentials
    {
        public const string CredentialHeader = "X-Edge-Redemption-Credential";
        public const string LauncherHeader = "X-OWS-Launcher-Credential";
        public const string AssignmentHeader = "X-OWS-Assignment-Credential";
        public const string LauncherVariable = "OWS_INSTANCE_LAUNCHER_CREDENTIAL";

        public static string Hash(string value) =>
            string.IsNullOrEmpty(value) || value.Length > 256 ? null :
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        public static bool Matches(string variable, string supplied)
        {
            var expected = Environment.GetEnvironmentVariable(variable);
            return expected is { Length: >= 32 and <= 256 } && Hash(supplied) is string hash &&
                CryptographicOperations.FixedTimeEquals(Convert.FromHexString(Hash(expected)), Convert.FromHexString(hash));
        }
    }

    public sealed class MapInstanceOperation
    {
        public Guid CustomerGuid { get; set; }
        public int MapInstanceId { get; set; }
        public int WorldServerId { get; set; }
        public int MapId { get; set; }
        public int Port { get; set; }
        public int CharacterId { get; set; }
        public Guid UserGuid { get; set; }
        public int EdgeServerId { get; set; }
    }
}
