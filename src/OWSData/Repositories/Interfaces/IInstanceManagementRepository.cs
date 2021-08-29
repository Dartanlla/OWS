using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OWSData.Repositories.Interfaces
{
    public interface IInstanceManagementRepository
    {
        Task<GetServerInstanceFromPort> GetServerInstanceFromPort(Guid customerGUID, string serverIP, int port);
        Task<IEnumerable<GetZoneInstancesForWorldServer>> GetZoneInstancesForWorldServer(Guid customerGUID, int worldServerID);
        Task<SuccessAndErrorMessage> SetZoneInstanceStatus(Guid customerGUID, int zoneInstanceID, int instanceStatus);
        Task<SuccessAndErrorMessage> ShutDownWorldServer(Guid customerGUID, int worldServerID);
        Task<int> StartWorldServer(Guid customerGUID, string ip);
        Task<SuccessAndErrorMessage> UpdateNumberOfPlayers(Guid customerGUID, string serverIP, int port, int numberOfPlayers);
    }
}
