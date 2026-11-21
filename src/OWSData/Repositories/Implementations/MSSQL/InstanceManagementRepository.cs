using Dapper;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSData.SQL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.MSSQL
{
    public class InstanceManagementRepository : IInstanceManagementRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public InstanceManagementRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        public IDbConnection Connection
        {
            get
            {
                return new SqlConnection(_storageOptions.Value.OWSDBConnectionString);
            }
        }

        public async Task<int> AddEdgeServer(Guid customerGuid, string host, int port, bool isActive)
        {
            using var connection = Connection;
            try
            {
                return await connection.QuerySingleOrDefaultAsync<int>(MSSQLQueries.AddEdgeServer,
                    new { CustomerGUID = customerGuid, Host = host, Port = port, IsActive = isActive });
            }
            catch (SqlException exception) when (exception.Number == 2627 &&
                exception.Message.Contains("UQ_EdgeServers_Endpoint", StringComparison.Ordinal))
            {
                throw new EdgeServerEndpointInUseException(exception);
            }
        }

        public async Task<EdgeServers> GetEdgeServer(Guid customerGuid, int edgeServerId)
        {
            using var connection = Connection;
            return await connection.QuerySingleOrDefaultAsync<EdgeServers>(GenericQueries.GetEdgeServer,
                new { CustomerGUID = customerGuid, EdgeServerID = edgeServerId });
        }

        public async Task<IEnumerable<EdgeServers>> GetEdgeServers(Guid customerGuid, bool? isActive)
        {
            using var connection = Connection;
            return await connection.QueryAsync<EdgeServers>(GenericQueries.GetEdgeServers,
                new { CustomerGUID = customerGuid, IsActive = isActive });
        }

        public async Task<bool> UpdateEdgeServer(Guid customerGuid, int edgeServerId, string host, int port, bool isActive)
        {
            using var connection = Connection;
            try
            {
                return await connection.ExecuteAsync(MSSQLQueries.UpdateEdgeServer,
                    new { CustomerGUID = customerGuid, EdgeServerID = edgeServerId, Host = host, Port = port, IsActive = isActive }) == 1;
            }
            catch (SqlException exception) when (exception.Number == 2627 &&
                exception.Message.Contains("UQ_EdgeServers_Endpoint", StringComparison.Ordinal))
            {
                throw new EdgeServerEndpointInUseException(exception);
            }
        }

        public async Task<bool> DeleteEdgeServer(Guid customerGuid, int edgeServerId)
        {
            using var connection = Connection;
            return await connection.ExecuteAsync(GenericQueries.DeleteEdgeServer,
                new { CustomerGUID = customerGuid, EdgeServerID = edgeServerId }) == 1;
        }

        public async Task<int> AddMapInstance(Guid tenant, int worldServerId, int mapId, int port)
        {
            using var connection = Connection;
            return await connection.QuerySingleOrDefaultAsync<int>(MSSQLQueries.AddMapInstanceForTenant,
                new { CustomerGUID = tenant, WorldServerID = worldServerId, MapID = mapId, Port = port, PlayerGroupID = (int?)null });
        }

        public async Task<IEnumerable<OWSData.Models.Tables.MapInstances>> GetReadyMapInstances(Guid tenant)
        {
            using var connection = Connection;
            return await connection.QueryAsync<OWSData.Models.Tables.MapInstances>(
                @"SELECT CustomerGUID, MapInstanceID, WorldServerID, MapID, Port, Status, PlayerGroupID,
                         NumberOfReportedPlayers, LastUpdateFromServer, LastServerEmptyDate
                  FROM MapInstances WHERE CustomerGUID = @tenant AND Status = 2", new { tenant });
        }

        public async Task<bool> HasReadyAssignment(Guid tenant, int characterId, Guid owner, int mapInstanceId, int edgeServerId)
        {
            using var connection = Connection;
            return await connection.QuerySingleAsync<long>(GenericQueries.HasReadyAssignment,
                new { CustomerGUID = tenant, CharacterID = characterId, UserGUID = owner, MapInstanceID = mapInstanceId, EdgeServerID = edgeServerId, Active = true }) > 0;
        }

        public async Task<bool> ProvisionEdgeServerCredential(Guid tenant, int edgeServerId, string hash)
        {
            using var connection = Connection;
            return await connection.ExecuteAsync(MSSQLQueries.ProvisionEdgeServerCredential,
                new { CustomerGUID = tenant, EdgeServerID = edgeServerId, Hash = hash, Inactive = false }) == 1;
        }

        public async Task<Guid?> AuthenticateActiveEdgeServer(int edgeServerId, string hash)
        {
            using var connection = Connection;
            return await connection.QuerySingleOrDefaultAsync<Guid?>(GenericQueries.AuthenticateActiveEdgeServer,
                new { EdgeServerID = edgeServerId, Hash = hash, Active = true });
        }

        public async Task<bool> SetEdgeServerReady(Guid tenant, int edgeServerId, string hash)
        {
            using var connection = Connection;
            return await connection.ExecuteAsync(MSSQLQueries.SetEdgeServerReady,
                new { CustomerGUID = tenant, EdgeServerID = edgeServerId, Hash = hash, Active = true }) == 1;
        }

        public async Task<bool> RevokeEdgeServerCredential(Guid tenant, int edgeServerId, string hash)
        {
            using var connection = Connection;
            return await connection.ExecuteAsync(MSSQLQueries.RevokeEdgeServerCredential,
                new { CustomerGUID = tenant, EdgeServerID = edgeServerId, Hash = hash, Inactive = false }) == 1;
        }

        public async Task<GetServerInstanceFromPort> GetZoneInstance(Guid customerGUID, int zoneInstanceId)
        {
            GetServerInstanceFromPort output;

            try
            {
                using (Connection)
                {
                    var parameter = new DynamicParameters();
                    parameter.Add("@CustomerGUID", customerGUID);
                    parameter.Add("@MapInstanceID", zoneInstanceId);

                    output = await Connection.QuerySingleAsync<GetServerInstanceFromPort>(GenericQueries.GetMapInstance,
                        parameter,
                        commandType: CommandType.Text);
                }

                return output;
            }
            catch (Exception ex)
            {
                output = new GetServerInstanceFromPort();
                return output;
            }
        }

        public async Task<GetServerInstanceFromPort> GetServerInstanceFromPort(Guid customerGUID, string serverIP, int port)
        {
            GetServerInstanceFromPort output;

            try
            {
                using (Connection)
                {
                    var parameter = new DynamicParameters();
                    parameter.Add("@CustomerGUID", customerGUID);
                    parameter.Add("@ServerIP", serverIP);
                    parameter.Add("@Port", port);

                    output = await Connection.QuerySingleAsync<GetServerInstanceFromPort>(GenericQueries.GetMapInstancesByIpAndPort,
                        parameter,
                        commandType: CommandType.Text);
                }

                return output;
            }
            catch (Exception ex) {
                output = new GetServerInstanceFromPort();
                return output;
            }
        }

        public async Task<IEnumerable<GetZoneInstancesForWorldServer>> GetZoneInstancesForWorldServer(Guid customerGUID, int worldServerID)
        {
            IEnumerable<GetZoneInstancesForWorldServer> output;

            using (Connection)
            {
                var parameter = new DynamicParameters();
                parameter.Add("@CustomerGUID", customerGUID);
                parameter.Add("@WorldServerID", worldServerID);

                output = await Connection.QueryAsync<GetZoneInstancesForWorldServer>(MSSQLQueries.GetMapInstancesByWorldServerID,
                    parameter,
                    commandType: CommandType.Text);
            }


            return output;
        }

        public async Task<SuccessAndErrorMessage> SetZoneInstanceStatus(Guid customerGUID, int zoneInstanceID, int instanceStatus)
        {
            using var connection = Connection;
            var updated = await connection.ExecuteAsync(GenericQueries.UpdateMapInstanceStatus, new
            {
                CustomerGUID = customerGUID, MapInstanceID = zoneInstanceID, MapInstanceStatus = instanceStatus
            });
            return new SuccessAndErrorMessage
            {
                Success = updated == 1,
                ErrorMessage = updated == 1 ? "" : "The instance could not be updated."
            };
        }

        public async Task<SuccessAndErrorMessage> ShutDownWorldServer(Guid customerGUID, int worldServerID)
        {
            IDbConnection conn = Connection;
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameter = new DynamicParameters();
                parameter.Add("@CustomerGUID", customerGUID);
                parameter.Add("@WorldServerID", worldServerID);
                parameter.Add("@ServerStatus", 0);

                await Connection.ExecuteAsync(GenericQueries.RemoveAllCharactersFromAllInstancesByWorldID,
                    parameter,
                    commandType: CommandType.Text);

                await Connection.ExecuteAsync(GenericQueries.UpdateWorldServerStatus,
                    parameter,
                    commandType: CommandType.Text);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw new Exception("Database Exception in ShutDownWorldServer!");
            }

            SuccessAndErrorMessage output = new SuccessAndErrorMessage()
            {
                Success = true,
                ErrorMessage = ""
            };

            return output;
        }

        public async Task<int> StartWorldServer(Guid customerGUID, string launcherGuid)
        {
            int worldServerId = -1;

            using (Connection)
            {
                var paremeters = new {
                    CustomerGUID = customerGUID,
                    ZoneServerGUID = launcherGuid
                };

                var getWorldServerID = await Connection.QueryFirstOrDefaultAsync<GetWorldServerID>(MSSQLQueries.GetWorldServerSQL, paremeters);

                if (getWorldServerID != null)
                {
                    worldServerId = getWorldServerID.WorldServerID;
                }

                if (worldServerId > 0)
                {
                    var paremeters2 = new {
                        CustomerGUID = customerGUID,
                        WorldServerID = worldServerId
                    };

                    await Connection.ExecuteAsync(MSSQLQueries.UpdateWorldServerSQL, paremeters2);
                }
            }

            return worldServerId;
        }

        public async Task<SuccessAndErrorMessage> UpdateNumberOfPlayers(Guid customerGUID, int zoneInstanceId, int numberOfPlayers)
        {
            using (Connection)
            {
                var paremeters = new
                {
                    CustomerGUID = customerGUID,
                    ZoneInstanceID = zoneInstanceId,
                    NumberOfReportedPlayers = numberOfPlayers
                };

                _ = await Connection.ExecuteAsync(MSSQLQueries.UpdateNumberOfPlayersSQL, paremeters);
            }

            SuccessAndErrorMessage output = new SuccessAndErrorMessage()
            {
                Success = true,
                ErrorMessage = ""
            };

            return output;

            /*
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@IP", serverIP);
                p.Add("@Port", port);
                p.Add("@NumberOfReportedPlayers", numberOfPlayers);

                await Connection.ExecuteAsync("UpdateNumberOfPlayers",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            SuccessAndErrorMessage output = new SuccessAndErrorMessage()
            {
                Success = true,
                ErrorMessage = ""
            };

            return output;*/
        }

        public async Task<IEnumerable<GetZoneInstancesForZone>> GetZoneInstancesOfZone(Guid customerGUID, string ZoneName)
        {
            IEnumerable<GetZoneInstancesForZone> output;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@ZoneName", ZoneName);

                output = await Connection.QueryAsync<GetZoneInstancesForZone>("GetZoneInstancesOfZone",
                    p,
                    commandType: CommandType.StoredProcedure);
            }


            return output;
        }

        public async Task<GetCurrentWorldTime> GetCurrentWorldTime(Guid customerGUID)
        {
            GetCurrentWorldTime output;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);

                output = await Connection.QuerySingleOrDefaultAsync<GetCurrentWorldTime>("GetWorldStartTime",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            return output;
        }

        public async Task<SuccessAndErrorMessage> RegisterLauncher(Guid customerGUID, string launcherGuid, string serverIp, int maxNumberOfInstances, string internalServerIp, int startingInstancePort)
        {
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@ZoneServerGUID", launcherGuid);
                    p.Add("@ServerIP", serverIp);
                    p.Add("@MaxNumberOfInstances", maxNumberOfInstances);
                    p.Add("@InternalServerIP", internalServerIp);
                    p.Add("@StartingMapInstancePort", startingInstancePort);

                    await Connection.ExecuteAsync(MSSQLQueries.AddOrUpdateWorldServerSQL,
                        p,
                        commandType: CommandType.Text);
                }

                SuccessAndErrorMessage output = new SuccessAndErrorMessage()
                {
                    Success = true,
                    ErrorMessage = ""
                };

                return output;
            }
            catch (Exception ex)
            {
                SuccessAndErrorMessage output = new SuccessAndErrorMessage()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                return output;
            }
        }

        public async Task<SuccessAndErrorMessage> AddZone(Guid customerGUID, string mapName, string zoneName, string worldCompContainsFilter, string worldCompListFilter, int softPlayerCap, int hardPlayerCap, int mapMode)
        {
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@MapID", 0);
                    p.Add("@MapName", mapName);
                    p.Add("@MapData", new byte[1]);
                    p.Add("@ZoneName", zoneName);
                    p.Add("@WorldCompContainsFilter", worldCompContainsFilter);
                    p.Add("@WorldCompListFilter", worldCompListFilter);
                    p.Add("@SoftPlayerCap", softPlayerCap);
                    p.Add("@HardPlayerCap", hardPlayerCap);
                    p.Add("@MapMode", mapMode);

                    await Connection.ExecuteAsync("AddOrUpdateMapZone",
                        p,
                        commandType: CommandType.StoredProcedure);
                }

                SuccessAndErrorMessage output = new SuccessAndErrorMessage()
                {
                    Success = true,
                    ErrorMessage = ""
                };

                return output;
            }
            catch (Exception ex)
            {
                SuccessAndErrorMessage output = new SuccessAndErrorMessage()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                return output;
            }
        }

        public async Task<SuccessAndErrorMessage> UpdateZone(Guid customerGUID, int mapId, string mapName, string zoneName, string worldCompContainsFilter, string worldCompListFilter, int softPlayerCap, int hardPlayerCap, int mapMode)
        {
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@MapID", mapId);
                    p.Add("@MapName", mapName);
                    p.Add("@MapData", new byte[1]);
                    p.Add("@ZoneName", zoneName);
                    p.Add("@WorldCompContainsFilter", worldCompContainsFilter);
                    p.Add("@WorldCompListFilter", worldCompListFilter);
                    p.Add("@SoftPlayerCap", softPlayerCap);
                    p.Add("@HardPlayerCap", hardPlayerCap);
                    p.Add("@MapMode", mapMode);

                    await Connection.ExecuteAsync("AddOrUpdateMapZone",
                        p,
                        commandType: CommandType.StoredProcedure);
                }

                SuccessAndErrorMessage output = new SuccessAndErrorMessage()
                {
                    Success = true,
                    ErrorMessage = ""
                };

                return output;
            }
            catch (Exception ex)
            {
                SuccessAndErrorMessage output = new SuccessAndErrorMessage()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                return output;
            }
        }
    }
}
