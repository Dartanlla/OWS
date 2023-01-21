using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSData.Repositories.Implementations.Postgres
{
    public class GlobalDataRepository : IGlobalDataRepository
    {
        public Task AddOrUpdateGlobalData(GlobalData globalData)
        {
            throw new NotImplementedException();
        }

        public Task<GlobalData> GetGlobalDataByGlobalDataKey(Guid customerGuid, string globalDataKey)
        {
            throw new NotImplementedException();
        }
    }
}
