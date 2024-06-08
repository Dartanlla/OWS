using Dapper;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSData.Repositories.Implementations.MSSQL
{
    public class ActionHouseRepository : IActionHouseRepository
    {
        private readonly IOptions<StorageOptions> _storageOptions;

        public ActionHouseRepository(IOptions<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        private IDbConnection Connection => new SqlConnection(_storageOptions.Value.OWSDBConnectionString);

        public async Task<ActionHousePlayerContainer> GetActionHousePlayerItems(Guid customerGUID, string characterName)
        {
            ActionHousePlayerContainer result = new ActionHousePlayerContainer();
            
            try
            {
                using (Connection)
                {
                    var p = new DynamicParameters();
                    p.Add("CustomerGUID", customerGUID);
                    p.Add("CharacterName", characterName);
                    p.Add("ErrorMessage", dbType: DbType.String, direction: ParameterDirection.Output, size: 50);

                    result.Items = await Connection.QueryAsync<ActionHousePlayerItem>("GetActionHousePlayerItems",
                    p, commandType: CommandType.StoredProcedure);

                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }
            return result;
        }

        public async Task GetActionHouseItemSearch(Guid customerGUID, string ItemSearch)
        {
            throw new NotImplementedException();
        }
    }
}
