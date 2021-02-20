using Dapper;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
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

        public async Task<SuccessAndErrorMessage> SetZoneInstanceStatus(Guid customerGUID, int zoneInstanceID, int instanceStatus)
        {
            using (Connection)
            {
                var p = new DynamicParameters();
                //p.Add("@CustomerGUID", customerGUID); //The Stored Procedure SetMapInstanceStatus mistakenly does not accept a CustomerGUID.  This is an error that will be corrected in the new distributed database(s).
                p.Add("@MapInstanceID", zoneInstanceID);
                p.Add("@MapInstanceStatus", instanceStatus);

                await Connection.QueryFirstOrDefaultAsync("SetMapInstanceStatus",
                    p,
                    commandType: CommandType.StoredProcedure);
            }

            SuccessAndErrorMessage Output = new SuccessAndErrorMessage()
            {
                Success = true,
                ErrorMessage = ""
            };

            return Output;
        }
    }
}
