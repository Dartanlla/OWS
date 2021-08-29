using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    /// <summary>
    /// GetServerInstanceFromPortRequest
    /// </summary>
    /// <remarks>
    /// Get information on the server instance that matches the port and IP
    /// </remarks>
    public class GetServerInstanceFromPortRequest
    {
        //Request Parameters
        public int Port { get; set; }

        //Private objects
        private Guid CustomerGUID;
        private IInstanceManagementRepository _instanceMangementRepository;
        private string _ip;

        public void SetData(IInstanceManagementRepository instanceMangementRepository, IHeaderCustomerGUID customerGuid, string ip)
        {
            CustomerGUID = customerGuid.CustomerGUID;
            _instanceMangementRepository = instanceMangementRepository;
            _ip = ip;
        }

        public async Task<IActionResult> Handle()
        {
            var output = await _instanceMangementRepository.GetServerInstanceFromPort(CustomerGUID, _ip, Port);

            return new OkObjectResult(output);
        }
    }
}
