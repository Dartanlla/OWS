using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    /// <summary>
    /// GetCurrentWorldTimeRequest
    /// </summary>
    /// <remarks>
    /// Get the Current World Time based on the StartTime stored in the WorldSettings table.  The format is in seconds since 1/1/1970.
    /// </remarks>
    public class GetCurrentWorldTimeRequest
    {
        //Request Parameters

        //Private objects
        private Guid CustomerGUID;
        private IInstanceManagementRepository _instanceMangementRepository;

        public void SetData(IInstanceManagementRepository instanceMangementRepository, IHeaderCustomerGUID customerGuid)
        {
            CustomerGUID = customerGuid.CustomerGUID;
            _instanceMangementRepository = instanceMangementRepository;
        }

        public async Task<IActionResult> Handle()
        {
            GetCurrentWorldTime output = await _instanceMangementRepository.GetCurrentWorldTime(CustomerGUID);

            return new OkObjectResult(output);
        }
    }
}
