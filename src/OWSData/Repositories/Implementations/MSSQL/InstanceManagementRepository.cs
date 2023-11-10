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
            using (Connection)
            {
                var parameter = new DynamicParameters();
                parameter.Add("@CustomerGUID", customerGUID);
                parameter.Add("@MapInstanceID", zoneInstanceID);
                parameter.Add("@MapInstanceStatus", instanceStatus);

                await Connection.QueryFirstOrDefaultAsync(GenericQueries.UpdateMapInstanceStatus,
                    parameter,
                    commandType: CommandType.Text);
            }

            SuccessAndErrorMessage output = new SuccessAndErrorMessage()
            {
                Success = true,
                ErrorMessage = ""
            };

            return output;
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
