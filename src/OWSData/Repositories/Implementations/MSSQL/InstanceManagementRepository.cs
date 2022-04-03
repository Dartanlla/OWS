using Dapper;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<GetServerInstanceFromPort> GetServerInstanceFromPort(Guid customerGUID, string serverIP, int port)
        {
            GetServerInstanceFromPort output;

            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@ServerIP", serverIP);
                    p.Add("@Port", port);

                    output = await Connection.QuerySingleAsync<GetServerInstanceFromPort>("GetServerInstanceFromIPAndPort",
                        p,
                        commandType: CommandType.StoredProcedure);
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
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@WorldServerID", worldServerID);

                output = await Connection.QueryAsync<GetZoneInstancesForWorldServer>("GetMapInstancesForWorldServerID",
                    p,
                    commandType: CommandType.StoredProcedure);
            }


            return output;
        }

        public async Task<SuccessAndErrorMessage> SetZoneInstanceStatus(Guid customerGUID, int zoneInstanceID, int instanceStatus)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@MapInstanceID", zoneInstanceID);
                p.Add("@MapInstanceStatus", instanceStatus);

                await Connection.QueryFirstOrDefaultAsync("SetMapInstanceStatus",
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

        public async Task<SuccessAndErrorMessage> ShutDownWorldServer(Guid customerGUID, int worldServerID)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@WorldServerID", worldServerID);

                await Connection.ExecuteAsync("ShutdownWorldServer",
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

        public async Task<int> StartWorldServer(Guid customerGUID, string ip)
        {
            int worldServerId;

            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@ServerIP", ip);
                p.Add("@WorldServerID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await Connection.ExecuteAsync("StartWorldServer",
                    p,
                    commandType: CommandType.StoredProcedure);

                worldServerId = p.Get<int>("@WorldServerID");
            }

            return worldServerId;
        }

        public async Task<SuccessAndErrorMessage> UpdateNumberOfPlayers(Guid customerGUID, string serverIP, int port, int numberOfPlayers)
        {
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

            return output;
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
