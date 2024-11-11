using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Requests.Zones
{
    public class GetMapInstancesRequest
    {
        private readonly Guid _customerGuid;
        private readonly IInstanceManagementRepository _zonesRepository;

        public GetMapInstancesRequest(Guid customerGuid, IInstanceManagementRepository zonesRepository)
        {
            _customerGuid = customerGuid;
            _zonesRepository = zonesRepository;
        }
        public async Task<IEnumerable<MapInstances>> Handle()
        {
            return await _zonesRepository.GetMapInstances(_customerGuid); ;
        }
    }
}
