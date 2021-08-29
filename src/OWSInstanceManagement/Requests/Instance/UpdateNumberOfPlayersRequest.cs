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
    /// UpdateNumberOfPlayersRequest
    /// </summary>
    /// <remarks>
    /// Update the number of players on a zone instance by matching it to the port and IP
    /// </remarks>
    public class UpdateNumberOfPlayersRequest
    {
        //Request Parameters
        public int Port { get; set; }
        public int NumberOfConnectedPlayers { get; set; }

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

        public async Task<IActionResult> Handle()
        {
            var output = await _instanceMangementRepository.UpdateNumberOfPlayers(CustomerGUID, _ip, Port, NumberOfConnectedPlayers);

            return new OkObjectResult(output);
        }
    }
}
