using Dapper;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSData.SQL;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using OWSShared.Options;

namespace OWSData.Repositories.Implementations.MSSQL
{
    public class GlobalDataRepository : IGlobalDataRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public GlobalDataRepository(IOptions<StorageOptions> storageOptions)
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


    }
}
