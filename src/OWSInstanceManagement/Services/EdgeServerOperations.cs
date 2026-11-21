using System;
using System.Net;
using System.Threading.Tasks;
using OWSData.Models;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;

namespace OWSInstanceManagement.Services
{
    public sealed class EdgeServerRequest
    {
        public Guid CustomerGuid { get; set; }
        public int EdgeServerId { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool? IsActive { get; set; }
        public string CredentialHash { get; set; }
    }

    public enum EdgeServerOperation
    {
        AddEdgeServer,
        GetEdgeServer,
        GetEdgeServers,
        UpdateEdgeServer,
        DeleteEdgeServer,
        ProvisionEdgeServerCredential,
        SetEdgeServerReady,
        RevokeEdgeServerCredential
    }

    public static class EdgeServerOperations
    {
        public static bool TryParseOperation(string name, out EdgeServerOperation operation) =>
            Enum.TryParse(name, ignoreCase: true, out operation) &&
            string.Equals(name, operation.ToString(), StringComparison.OrdinalIgnoreCase) &&
            Enum.IsDefined(operation);

        public static async Task<(int Status, object Payload)> ExecuteAsync(EdgeServerOperation operation,
            EdgeServerRequest request, string launcherCredential, string edgeCredential, IInstanceManagementRepository repository)
        {
            var launcherAuthorized = ServerCredentials.Matches(ServerCredentials.LauncherVariable, launcherCredential);
            var useEdgeCredential = operation == EdgeServerOperation.SetEdgeServerReady ||
                (operation == EdgeServerOperation.RevokeEdgeServerCredential && !launcherAuthorized);
            var hash = useEdgeCredential ? ServerCredentials.Hash(edgeCredential) : null;
            if (useEdgeCredential && hash == null)
                return (401, new { ErrorMessage = "An Edge server credential is required." });
            if (!useEdgeCredential && !launcherAuthorized)
                return (401, new { ErrorMessage = "Invalid launcher credential." });
            if (request == null || request.CustomerGuid == Guid.Empty)
                return (400, new { ErrorMessage = "A tenant is required." });
            if (operation is not (EdgeServerOperation.AddEdgeServer or EdgeServerOperation.GetEdgeServers)
                && request.EdgeServerId <= 0)
                return (400, new { ErrorMessage = "A positive Edge server ID is required." });

            var customer = request.CustomerGuid;
            var id = request.EdgeServerId;
            if (operation == EdgeServerOperation.ProvisionEdgeServerCredential)
            {
                if (request.CredentialHash?.Length != 64 || !Array.TrueForAll(request.CredentialHash.ToCharArray(), Uri.IsHexDigit))
                    return (400, new { ErrorMessage = "A SHA-256 credential hash is required." });
                return await repository.ProvisionEdgeServerCredential(customer, id, request.CredentialHash.ToUpperInvariant())
                    ? (200, new { Success = true }) : (409, new { ErrorMessage = "Edge server is unavailable for provisioning." });
            }
            if (operation == EdgeServerOperation.SetEdgeServerReady)
                return await repository.SetEdgeServerReady(customer, id, hash)
                    ? (200, new { Success = true }) : InvalidEdgeCredential();
            if (operation == EdgeServerOperation.RevokeEdgeServerCredential)
                return await repository.RevokeEdgeServerCredential(customer, id, hash)
                    ? (200, new { Success = true }) : launcherAuthorized ? NotFound() : InvalidEdgeCredential();
            if (operation == EdgeServerOperation.GetEdgeServers)
                return (200, await repository.GetEdgeServers(customer, request.IsActive));
            if (operation == EdgeServerOperation.GetEdgeServer)
            {
                var server = await repository.GetEdgeServer(customer, id);
                return server == null ? NotFound() : (200, server);
            }
            if (operation == EdgeServerOperation.DeleteEdgeServer)
                return await repository.DeleteEdgeServer(customer, id) ? (200, new { Success = true }) : NotFound();

            var host = NormalizeHost(request.Host);
            if (host == null || request.Port is < 1 or > 65535)
                return (400, new { ErrorMessage = "An IP address or DNS hostname and a valid port are required." });

            try
            {
                if (operation == EdgeServerOperation.AddEdgeServer)
                {
                    var newId = await repository.AddEdgeServer(customer, host, request.Port, request.IsActive ?? false);
                    return newId > 0 ? (201, new { EdgeServerId = newId }) : (404, new { ErrorMessage = "Customer not found." });
                }
                if (!request.IsActive.HasValue)
                    return (400, new { ErrorMessage = "IsActive is required when updating an Edge server." });
                return await repository.UpdateEdgeServer(customer, id, host, request.Port, request.IsActive.Value)
                    ? (200, new { Success = true }) : NotFound();
            }
            catch (EdgeServerEndpointInUseException)
            {
                return (409, new { ErrorMessage = "An Edge server already uses this host and port for the customer." });
            }
        }

        private static (int Status, object Payload) NotFound() => (404, new { ErrorMessage = "Edge server not found." });

        private static (int Status, object Payload) InvalidEdgeCredential() =>
            (401, new { ErrorMessage = "Invalid Edge server credential." });

        private static string NormalizeHost(string value)
        {
            var host = value?.Trim();
            if (string.IsNullOrEmpty(host) || host.Length > 254 || host.Contains('%'))
                return null;
            if (IPAddress.TryParse(host, out var address))
                return address.ToString();

            host = host.EndsWith('.') ? host[..^1] : host;
            return host.Length <= 253 && Uri.CheckHostName(host) == UriHostNameType.Dns && Array.TrueForAll(host.ToCharArray(), char.IsAscii)
                ? host.ToLowerInvariant() : null;
        }
    }
}
