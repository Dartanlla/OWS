using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Npgsql;
using System.Threading;
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

        private readonly AsyncLocal<ScopedDbConnection> _scopedConnection = new AsyncLocal<ScopedDbConnection>();

        private DbConnection CreateConnection()
        {
            return new NpgsqlConnection(PostgresConnectionString.WithPoolDefaults(_storageOptions.Value.OWSDBConnectionString));
        }

        private DbConnection Connection => _scopedConnection.Value ??= new ScopedDbConnection(CreateConnection(), () => _scopedConnection.Value = null);

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

                output = await Connection.QueryAsync<GetZoneInstancesForWorldServer>(PostgresQueries.GetMapInstancesByWorldServerID,
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

        public async Task<SuccessAndErrorMessage> CompleteZoneInstanceShutdown(Guid customerGUID, int zoneInstanceID)
        {
            const int ZoneInstanceShuttingDownStatus = 3;

            using IDbConnection conn = CreateConnection();
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CustomerGUID", customerGUID);
                parameters.Add("@ZoneInstanceID", zoneInstanceID);

                int? status = await conn.QueryFirstOrDefaultAsync<int?>(PostgresQueries.GetZoneInstanceStatusForShutdown,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.Text);

                if (!status.HasValue)
                {
                    transaction.Rollback();
                    return new SuccessAndErrorMessage()
                    {
                        Success = false,
                        ErrorMessage = $"Zone Instance {zoneInstanceID} was not found."
                    };
                }

                if (status.Value != ZoneInstanceShuttingDownStatus)
                {
                    transaction.Rollback();
                    return new SuccessAndErrorMessage()
                    {
                        Success = false,
                        ErrorMessage = $"Zone Instance {zoneInstanceID} is not shutting down."
                    };
                }

                await conn.ExecuteAsync(PostgresQueries.RemoveCharactersFromZoneInstance,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.Text);

                int removedZoneInstances = await conn.ExecuteAsync(PostgresQueries.RemoveZoneInstance,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.Text);

                if (removedZoneInstances != 1)
                {
                    transaction.Rollback();
                    return new SuccessAndErrorMessage()
                    {
                        Success = false,
                        ErrorMessage = $"Zone Instance {zoneInstanceID} could not be removed."
                    };
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                return new SuccessAndErrorMessage()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }

            return new SuccessAndErrorMessage()
            {
                Success = true,
                ErrorMessage = ""
            };
        }

        public async Task<SuccessAndErrorMessage> ShutDownWorldServer(Guid customerGUID, int worldServerID)
        {
            using IDbConnection conn = CreateConnection();
            conn.Open();
            using IDbTransaction transaction = conn.BeginTransaction();
            try
            {
                var parameter = new DynamicParameters();
                parameter.Add("@CustomerGUID", customerGUID);
                parameter.Add("@WorldServerID", worldServerID);
                parameter.Add("@ServerStatus", 0);

                await conn.ExecuteAsync(GenericQueries.RemoveAllCharactersFromAllInstancesByWorldID,
                    parameter,
                    transaction: transaction,
                    commandType: CommandType.Text);

                await conn.ExecuteAsync(GenericQueries.RemoveAllMapInstancesForWorldServer,
                    parameter,
                    transaction: transaction,
                    commandType: CommandType.Text);

                await conn.ExecuteAsync(GenericQueries.UpdateWorldServerStatus,
                    parameter,
                    transaction: transaction,
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
                var parameters = new {
                    CustomerGUID = customerGUID,
                    ZoneServerGUID = launcherGuid
                };

                GetWorldServerID getWorldServerID  = await Connection.QueryFirstOrDefaultAsync<GetWorldServerID>(PostgresQueries.GetWorldServerSQL, parameters);

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

                    await Connection.ExecuteAsync(PostgresQueries.UpdateWorldServerSQL, parameters2);
                }
            }

            return worldServerId;
        }

        public async Task<SuccessAndErrorMessage> UpdateNumberOfPlayers(Guid customerGUID, int zoneInstanceId, int numberOfPlayers, ZoneServerStats stats)
        {
            using (Connection)
            {
                var paremeters = new
                {
                    CustomerGUID = customerGUID,
                    ZoneInstanceID = zoneInstanceId,
                    NumberOfReportedPlayers = numberOfPlayers,
                    stats.ServerFrameMsAvg,
                    stats.ServerFrameMsMax,
                    stats.ServerCpuPct,
                    stats.MemoryUsedMB,
                    stats.UptimeSeconds,
                    stats.BuildCL
                };

                _ = await Connection.ExecuteAsync(PostgresQueries.UpdateNumberOfPlayersSQL, paremeters);
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

                output = await Connection.QueryAsync<GetZoneInstancesForZone>(PostgresQueries.GetZoneInstancesOfZone,
                    p,
                    commandType: CommandType.Text);
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

                output = await Connection.QuerySingleOrDefaultAsync<GetCurrentWorldTime>("select * from GetWorldStartTime(@CustomerGUID)",
                    p,
                    commandType: CommandType.Text);
            }

            return output;
        }

        public async Task<SuccessAndErrorMessage> RegisterLauncher(Guid customerGUID, string launcherGuid, string serverIp, int maxNumberOfInstances, string internalServerIp, int startingInstancePort, string serverRegion)
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
                    p.Add("@ServerRegion", ServerRegions.Normalize(serverRegion));

                    await Connection.ExecuteAsync(PostgresQueries.AddOrUpdateWorldServerSQL,
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

        public async Task<SuccessAndErrorMessage> AddZone(Guid customerGUID, string mapName, string zoneName, string worldCompContainsFilter, string worldCompListFilter, int softPlayerCap, int hardPlayerCap, int mapMode, int minutesToShutdownAfterEmpty)
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
                    p.Add("@MinutesToShutdownAfterEmpty", minutesToShutdownAfterEmpty);

                    // Nothing in the schema prevents a second row with this ZoneName, and the
                    // launcher resolves zones by name, so check before inserting.
                    if (await ZoneNameIsTaken(customerGUID, zoneName, 0))
                    {
                        return new SuccessAndErrorMessage()
                        {
                            Success = false,
                            ErrorMessage = $"A zone named '{zoneName}' already exists."
                        };
                    }

                    await Connection.ExecuteAsync(PostgresQueries.AddZone,
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

        public async Task<SuccessAndErrorMessage> UpdateZone(Guid customerGUID, int mapId, string mapName, string zoneName, string worldCompContainsFilter, string worldCompListFilter, int softPlayerCap, int hardPlayerCap, int mapMode, int minutesToShutdownAfterEmpty)
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
                    p.Add("@MinutesToShutdownAfterEmpty", minutesToShutdownAfterEmpty);

                    // Excluding this MapID lets a row keep its own name while still blocking a
                    // rename onto another row's name.
                    if (await ZoneNameIsTaken(customerGUID, zoneName, mapId))
                    {
                        return new SuccessAndErrorMessage()
                        {
                            Success = false,
                            ErrorMessage = $"Another zone named '{zoneName}' already exists."
                        };
                    }

                    await Connection.ExecuteAsync(PostgresQueries.UpdateZone,
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

        public async Task<IEnumerable<ZoneSummary>> GetZones(Guid customerGUID)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);

                return await Connection.QueryAsync<ZoneSummary>(GenericQueries.GetZones,
                    p,
                    commandType: CommandType.Text);
            }
        }

        public async Task<IEnumerable<ZoneInstanceSummary>> GetLiveZoneInstances(Guid customerGUID)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                p.Add("@CustomerGUID", customerGUID);

                return await Connection.QueryAsync<ZoneInstanceSummary>(PostgresQueries.GetLiveZoneInstances,
                    p,
                    commandType: CommandType.Text);
            }
        }

        public async Task<SuccessAndErrorMessage> DeleteZone(Guid customerGUID, int mapId)
        {
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("@CustomerGUID", customerGUID);
                    p.Add("@MapID", mapId);

                    // MapInstances has no cascade to Maps, so deleting a zone out from under a
                    // live instance leaves rows the launcher can no longer resolve a name for.
                    int liveInstances = await Connection.ExecuteScalarAsync<int>(GenericQueries.CountMapInstancesForMap,
                        p,
                        commandType: CommandType.Text);

                    if (liveInstances > 0)
                    {
                        return new SuccessAndErrorMessage()
                        {
                            Success = false,
                            ErrorMessage = $"This zone still has {liveInstances} map instance(s). Shut them down first."
                        };
                    }

                    int rowsAffected = await Connection.ExecuteAsync(GenericQueries.DeleteZone,
                        p,
                        commandType: CommandType.Text);

                    return new SuccessAndErrorMessage()
                    {
                        Success = rowsAffected > 0,
                        ErrorMessage = rowsAffected > 0 ? "" : "No zone found with that MapID."
                    };
                }
            }
            catch (Exception ex)
            {
                return new SuccessAndErrorMessage()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<bool> ZoneNameIsTaken(Guid customerGUID, string zoneName, int excludeMapId)
        {
            var p = new DynamicParameters();
            p.Add("@CustomerGUID", customerGUID);
            p.Add("@ZoneName", zoneName);
            p.Add("@ExcludeMapID", excludeMapId);

            int matches = await Connection.ExecuteScalarAsync<int>(GenericQueries.CountZonesWithZoneName,
                p,
                commandType: CommandType.Text);

            return matches > 0;
        }
    }
}
