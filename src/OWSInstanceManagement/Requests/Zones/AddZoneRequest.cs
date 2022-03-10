using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.RequestPayloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Zones
{
    public class AddZoneRequest
    {
        public AddOrUpdateZoneRequestPayload addOrUpdateZone { get; set; }

        private Guid customerGUID;
        private IInstanceManagementRepository instanceManagementRepository;

        public void SetData(IInstanceManagementRepository instanceManagementRepository, IHeaderCustomerGUID customerGuid)
        {
            this.instanceManagementRepository = instanceManagementRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            SuccessAndErrorMessage successAndErrorMessage = new SuccessAndErrorMessage();

            successAndErrorMessage = await instanceManagementRepository.AddZone(customerGUID, addOrUpdateZone.MapName, addOrUpdateZone.ZoneName, addOrUpdateZone.WorldCompContainsFilter, addOrUpdateZone.WorldCompListFilter, addOrUpdateZone.SoftPlayerCap, addOrUpdateZone.HardPlayerCap, 
                addOrUpdateZone.MapMode);

            return new OkObjectResult(successAndErrorMessage);
        }
    }
}
