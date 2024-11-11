using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Requests.Worlds
{
    public class GetWorldServersRequest
    {
        private readonly Guid _customerGuid;
        private readonly IInstanceManagementRepository _zonesRepository;

        public GetWorldServersRequest(Guid customerGuid, IInstanceManagementRepository zonesRepository)
        {
            _customerGuid = customerGuid;
            _zonesRepository = zonesRepository;
        }
        public async Task<IEnumerable<WorldServers>> Handle()
        {
            return await _zonesRepository.GetWorldServers(_customerGuid); ;
        }
    }
}
