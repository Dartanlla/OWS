using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSData.SQL;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class InstanceManagementRepository : IInstanceManagementRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public InstanceManagementRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private IDbConnection Connection => new NpgsqlConnection(_storageOptions.Value.OWSDBConnectionString);

        public async Task<GetServerInstanceFromPort> GetZoneInstance(Guid customerGUID, int zoneInstanceId)
        {
            GetServerInstanceFromPort output;

            try
            {
                using (var connection = (NpgsqlConnection)Connection)
                {
                    var parameter = new DynamicParameters();
                    parameter.Add("@CustomerGUID", customerGUID);
                    parameter.Add("@MapInstanceID", zoneInstanceId);

                    output = await connection.QuerySingleAsync<GetServerInstanceFromPort>(GenericQueries.GetMapInstance,
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
                using (var connection = (NpgsqlConnection)Connection)
                {
                    var parameter = new DynamicParameters();
                    parameter.Add("@CustomerGUID", customerGUID);
                    parameter.Add("@ServerIP", serverIP);
                    parameter.Add("@Port", port);

                    output = await connection.QuerySingleAsync<GetServerInstanceFromPort>(GenericQueries.GetMapInstancesByIpAndPort,
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

            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameter = new DynamicParameters();
                parameter.Add("@CustomerGUID", customerGUID);
                parameter.Add("@WorldServerID", worldServerID);

                output = await connection.QueryAsync<GetZoneInstancesForWorldServer>(PostgresQueries.GetMapInstancesByWorldServerID,
                    parameter,
                    commandType: CommandType.Text);
            }


            return output;
        }

        public async Task<SuccessAndErrorMessage> SetZoneInstanceStatus(Guid customerGUID, int zoneInstanceID, int instanceStatus)
        {
            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameter = new DynamicParameters();
                parameter.Add("@CustomerGUID", customerGUID);
                parameter.Add("@MapInstanceID", zoneInstanceID);
                parameter.Add("@MapInstanceStatus", instanceStatus);

                await connection.QueryFirstOrDefaultAsync(GenericQueries.UpdateMapInstanceStatus,
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
            using (var connection = (NpgsqlConnection)Connection)
            {
                await connection.OpenAsync();

                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var parameter = new DynamicParameters();
                        parameter.Add("@CustomerGUID", customerGUID);
                        parameter.Add("@WorldServerID", worldServerID);
                        parameter.Add("@ServerStatus", 0);

                        await connection.ExecuteAsync(GenericQueries.RemoveAllCharactersFromAllInstancesByWorldID,
                            parameter,
                            commandType: CommandType.Text);

                        await connection.ExecuteAsync(GenericQueries.RemoveAllMapInstancesForWorldServer,
                            parameter,
                            commandType: CommandType.Text);

                        await connection.ExecuteAsync(GenericQueries.UpdateWorldServerStatus,
                            parameter,
                            commandType: CommandType.Text);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw new Exception("Database Exception in ShutDownWorldServer!");
                    }
                }
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

            using (var connection = (NpgsqlConnection)Connection)
            {
                var parameters = new {
                    CustomerGUID = customerGUID,
                    ZoneServerGUID = launcherGuid
                };

                GetWorldServerID getWorldServerID  = await connection.QueryFirstOrDefaultAsync<GetWorldServerID>(PostgresQueries.GetWorldServerSQL, parameters);

                if (getWorldServerID != null)
                {
                    worldServerId = getWorldServerID.WorldServerID;
                }

                if (worldServerId > 0)
                {
                    var parameters2 = new {
                        CustomerGUID = customerGUID,
                        WorldServerID = worldServerId
                    };

                    await connection.ExecuteAsync(PostgresQueries.UpdateWorldServerSQL, parameters2);
                }
            }

            return worldServerId;
        }

        public async Task<SuccessAndErrorMessage> UpdateNumberOfPlayers(Guid customerGUID, int zoneInstanceId, int numberOfPlayers)
        {
            using (var connection = (NpgsqlConnection)Connection)
            {
                var paremeters = new
                {
                    CustomerGUID = customerGUID,
                    ZoneInstanceID = zoneInstanceId,
                    NumberOfReportedPlayers = numberOfPlayers
                };

                _ = await connection.ExecuteAsync(PostgresQueries.UpdateNumberOfPlayersSQL, paremeters);
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

            using (var connection = (NpgsqlConnection)Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);
                p.Add("@ZoneName", ZoneName);

                output = await connection.QueryAsync<GetZoneInstancesForZone>("select * from GetZoneInstancesOfZone(@CustomerGUID,@ZoneName)",
                    p,
                    commandType: CommandType.Text);
            }


            return output;
        }

        public async Task<GetCurrentWorldTime> GetCurrentWorldTime(Guid customerGUID)
        {
            GetCurrentWorldTime output;

            using (var connection = (NpgsqlConnection)Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);

                output = await connection.QuerySingleOrDefaultAsync<GetCurrentWorldTime>("select * from GetWorldStartTime(@CustomerGUID)",
                    p,
                    commandType: CommandType.Text);
            }

            return output;
        }

        public async Task<SuccessAndErrorMessage> RegisterLauncher(Guid customerGUID, string launcherGuid, string serverIp, int maxNumberOfInstances, string internalServerIp, int startingInstancePort)
        {
            try
            {
                using (var connection = (NpgsqlConnection)Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@ZoneServerGUID", launcherGuid);
                    p.Add("@ServerIP", serverIp);
                    p.Add("@MaxNumberOfInstances", maxNumberOfInstances);
                    p.Add("@InternalServerIP", internalServerIp);
                    p.Add("@StartingMapInstancePort", startingInstancePort);

                    await connection.ExecuteAsync(PostgresQueries.AddOrUpdateWorldServerSQL,
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
                using (var connection = (NpgsqlConnection)Connection)
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

                    await connection.ExecuteAsync("call AddOrUpdateMapZone(@CustomerGUID,@MapID,@MapName,@MapData,@ZoneName,@WorldCompContainsFilter,@WorldCompListFilter,@SoftPlayerCap,@HardPlayerCap,@MapMode)",
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

        public async Task<SuccessAndErrorMessage> UpdateZone(Guid customerGUID, int mapId, string mapName, string zoneName, string worldCompContainsFilter, string worldCompListFilter, int softPlayerCap, int hardPlayerCap, int mapMode)
        {
            try
            {
                using (var connection = (NpgsqlConnection)Connection)
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

                    await connection.ExecuteAsync("call AddOrUpdateMapZone(@CustomerGUID,@MapID,@MapName,@MapData,@ZoneName,@WorldCompContainsFilter,@WorldCompListFilter,@SoftPlayerCap,@HardPlayerCap,@MapMode)",
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
    }
}
