using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSGlobalData.DTOs;
using OWSShared.Interfaces;
using System;
using System.Threading.Tasks;

namespace OWSGlobalData.Requests
{
    public class AddOrUpdateGlobalDataItemRequest
    {
        private readonly AddOrUpdateGlobalDataItemDTO _dto;
        private readonly Guid _CustomerGUID;
        private readonly IGlobalDataRepository _globalDataRepository;

        public AddOrUpdateGlobalDataItemRequest(AddOrUpdateGlobalDataItemDTO dto, IGlobalDataRepository globalDataRepository, IHeaderCustomerGUID headerCustomerGUID)
        {
            _dto = dto;
            _globalDataRepository = globalDataRepository;
            _CustomerGUID = headerCustomerGUID.CustomerGUID;
        }

        public async Task<SuccessAndErrorMessage> Handle()
        {
            var globalDataToAdd = new GlobalData() {
                CustomerGuid = _CustomerGUID,
                GlobalDataKey = _dto.GlobalDataKey,
                GlobalDataValue = _dto.GlobalDataValue
            };

            await _globalDataRepository.AddOrUpdateGlobalData(globalDataToAdd);

            var successAndErrorMessage = new SuccessAndErrorMessage() {
                Success = true,
                ErrorMessage = ""
            };

            return successAndErrorMessage;
        }
    }
}
