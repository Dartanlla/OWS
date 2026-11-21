using System;
using System.Threading.Tasks;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;

namespace OWSInstanceManagement.Services
{
    public static class MapInstanceOperations
    {
        public static async Task<(int Status, object Payload)> ExecuteAsync(string action, MapInstanceOperation request,
            string launcherCredential, string assignmentCredential, IInstanceManagementRepository repository)
        {
            if (request == null || request.CustomerGuid == Guid.Empty)
                return (400, new { ErrorMessage = "A tenant is required." });

            if (action == "CheckReadyAssignment")
            {
                if (!ServerCredentials.Matches("OWS_INSTANCE_ASSIGNMENT_CREDENTIAL", assignmentCredential))
                    return (401, new { ErrorMessage = "Invalid assignment credential." });
                if (request.MapInstanceId <= 0 || request.EdgeServerId <= 0 || request.CharacterId <= 0 || request.UserGuid == Guid.Empty)
                    return (400, new { ErrorMessage = "Invalid assignment." });
                return (200, await repository.HasReadyAssignment(request.CustomerGuid, request.CharacterId,
                    request.UserGuid, request.MapInstanceId, request.EdgeServerId));
            }

            if (!ServerCredentials.Matches(ServerCredentials.LauncherVariable, launcherCredential))
                return (401, new { ErrorMessage = "Invalid launcher credential." });
            switch (action)
            {
                case "GetReadyMapInstances":
                    return (200, await repository.GetReadyMapInstances(request.CustomerGuid));
                case "AddMapInstance":
                    if (request.WorldServerId <= 0 || request.MapId <= 0 || request.Port <= 0 || request.Port > 65535)
                        return (400, new { ErrorMessage = "A world server, map, and valid port are required." });
                    var id = await repository.AddMapInstance(request.CustomerGuid, request.WorldServerId, request.MapId, request.Port);
                    return id > 0 ? (201, new { MapInstanceId = id }) : (404, new { ErrorMessage = "World server or map not found." });
                default:
                    return (404, new { ErrorMessage = "Unknown operation." });
            }
        }
    }
}
