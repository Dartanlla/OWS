using OWSData.Models.Composites;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSGlobalData.DTOs;
using OWSShared.Interfaces;
using System.Threading.Tasks;
using System;

namespace OWSGlobalData.Requests
{
    public class GetGlobalDataItemRequest
    {
        private readonly string _globalDataKey;
        private readonly Guid _CustomerGUID;
        private readonly IGlobalDataRepository _globalDataRepository;

        public GetGlobalDataItemRequest(string globalDataKey, IGlobalDataRepository globalDataRepository, IHeaderCustomerGUID headerCustomerGUID)
        {
            _globalDataKey = globalDataKey;
            _globalDataRepository = globalDataRepository;
            _CustomerGUID = headerCustomerGUID.CustomerGUID;
        }

        public async Task<GlobalData> Handle()
        {
            return await _globalDataRepository.GetGlobalDataByGlobalDataKey(_CustomerGUID, _globalDataKey);
        }
    }
}
