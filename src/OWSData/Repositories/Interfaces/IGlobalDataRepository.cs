using OWSData.Models.Composites;
using OWSData.Models.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWSData.Repositories.Interfaces
{
    public interface IGlobalDataRepository
    {
        Task AddOrUpdateGlobalData(GlobalData globalData);
        Task<GlobalData> GetGlobalDataByGlobalDataKey(Guid customerGuid, string globalDataKey);
    }
}
