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
    }
}
