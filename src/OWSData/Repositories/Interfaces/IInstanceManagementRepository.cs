using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OWSData.Repositories.Interfaces
{
    public interface IInstanceManagementRepository
    {
        Task<int> AddEdgeServer(Guid customerGuid, string host, int port, bool isActive);
        Task<EdgeServers> GetEdgeServer(Guid customerGuid, int edgeServerId);
        Task<IEnumerable<EdgeServers>> GetEdgeServers(Guid customerGuid, bool? isActive);
        Task<bool> UpdateEdgeServer(Guid customerGuid, int edgeServerId, string host, int port, bool isActive);
        Task<bool> DeleteEdgeServer(Guid customerGuid, int edgeServerId);
        Task<int> AddMapInstance(Guid tenant, int worldServerId, int mapId, int port);
        Task<IEnumerable<OWSData.Models.Tables.MapInstances>> GetReadyMapInstances(Guid tenant);
        Task<bool> HasReadyAssignment(Guid tenant, int characterId, Guid owner, int mapInstanceId, int edgeServerId);
        Task<bool> ProvisionEdgeServerCredential(Guid tenant, int edgeServerId, string hash);
        Task<Guid?> AuthenticateActiveEdgeServer(int edgeServerId, string hash);
        Task<bool> SetEdgeServerReady(Guid tenant, int edgeServerId, string hash);
        Task<bool> RevokeEdgeServerCredential(Guid tenant, int edgeServerId, string hash);
        Task<GetServerInstanceFromPort> GetZoneInstance(Guid customerGUID, int zoneInstanceId);
        Task<GetServerInstanceFromPort> GetServerInstanceFromPort(Guid customerGUID, string serverIP, int port);
        Task<IEnumerable<GetZoneInstancesForWorldServer>> GetZoneInstancesForWorldServer(Guid customerGUID, int worldServerID);
        Task<SuccessAndErrorMessage> SetZoneInstanceStatus(Guid customerGUID, int zoneInstanceID, int instanceStatus);
        Task<SuccessAndErrorMessage> ShutDownWorldServer(Guid customerGUID, int worldServerID);
        Task<int> StartWorldServer(Guid customerGUID, string launcherGuid);
        Task<SuccessAndErrorMessage> UpdateNumberOfPlayers(Guid customerGUID, int zoneInstanceId, int numberOfPlayers);
        Task<IEnumerable<GetZoneInstancesForZone>> GetZoneInstancesOfZone(Guid customerGUID, string ZoneName);
        Task<GetCurrentWorldTime> GetCurrentWorldTime(Guid customerGUID);
        Task<SuccessAndErrorMessage> RegisterLauncher(Guid customerGUID, string launcherGuid, string serverIp, int maxNumberOfInstances, string internalServerIp, int startingInstancePort);
        Task<SuccessAndErrorMessage> AddZone(Guid customerGUID, string mapName,	string zoneName, string worldCompContainsFilter, string worldCompListFilter, int softPlayerCap, int hardPlayerCap, int mapMode);
        Task<SuccessAndErrorMessage> UpdateZone(Guid customerGUID, int mapId, string mapName, string zoneName, string worldCompContainsFilter, string worldCompListFilter, int softPlayerCap, int hardPlayerCap, int mapMode);
    }
}
