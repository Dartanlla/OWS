using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Requests.Zones
{
    public class GetZonesRequest
    {
        private readonly Guid _customerGuid;
        private readonly IInstanceManagementRepository _zonesRepository;

        public GetZonesRequest(Guid customerGuid, IInstanceManagementRepository zonesRepository)
        {
            _customerGuid = customerGuid;
            _zonesRepository = zonesRepository;
        }
        public async Task<IEnumerable<Maps>> Handle()
        {
            return await _zonesRepository.GetZones(_customerGuid); ;
        }
    }
}
