using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
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

            //If running locally in docker the IP will contain "::", so switch it to 127.0.0.1 as this is running locally
            if (ip.Contains("::"))
            {
                _ip = "127.0.0.1";
            }
            else
            {
                _ip = ip;
            }
        }

        public async Task<GetServerInstanceFromPort> Handle()
        {
            GetServerInstanceFromPort output = await _instanceMangementRepository.GetServerInstanceFromPort(CustomerGUID, _ip, Port);

            return output;
        }
    }
}
