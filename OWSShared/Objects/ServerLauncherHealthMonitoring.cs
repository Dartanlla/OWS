using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OWSData.Models;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.Messages;
using OWSShared.RequestPayloads;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OWSShared.Objects
{
    public class ServerLauncherHealthMonitoring : IServerHealthMonitoringJob
    {
        private readonly IOptions<OWSInstanceLauncherOptions> _OWSInstanceLauncherOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IZoneServerProcessesRepository _zoneServerProcessesRepository;

        public ServerLauncherHealthMonitoring(IOptions<OWSInstanceLauncherOptions> OWSInstanceLauncherOptions, IHttpClientFactory httpClientFactory, IZoneServerProcessesRepository zoneServerProcessesRepository)
        {
            _OWSInstanceLauncherOptions = OWSInstanceLauncherOptions;
            _httpClientFactory = httpClientFactory;
            _zoneServerProcessesRepository = zoneServerProcessesRepository;

        }
        public void DoWork()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Server Health Monitoring is checking for broken Zone Server Instances...");
            Console.ForegroundColor = ConsoleColor.White;

            //Get a list of ZoneInstances from api/Instance/GetZoneInstancesForWorldServer
            List<GetZoneInstancesForWorldServer> zoneInstances = GetZoneInstancesForWorldServer(86);

        }

        public void Dispose()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Shutting Down OWS Server Health Monitoring...");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private List<GetZoneInstancesForWorldServer> GetZoneInstancesForWorldServer(int worldServerId)
        {
            List<GetZoneInstancesForWorldServer> output;

            var instanceManagementHttpClient = _httpClientFactory.CreateClient("OWSInstanceManagement");

            var worldServerIDRequestPayload = new
            {
                request = new WorldServerIDRequestPayload
                {
                    WorldServerID = worldServerId
                }
            };

            var shutDownInstanceLauncherRequest = new StringContent(JsonConvert.SerializeObject(worldServerIDRequestPayload), Encoding.UTF8, "application/json");

            var responseMessageTask = instanceManagementHttpClient.PostAsync("api/Instance/GetZoneInstancesForWorldServer", shutDownInstanceLauncherRequest);
            var responseMessage = responseMessageTask.Result;

            if (responseMessage.IsSuccessStatusCode)
            {
                var responseContentAsync = responseMessage.Content.ReadAsStringAsync();
                string responseContentString = responseContentAsync.Result;
                output = JsonConvert.DeserializeObject<List<GetZoneInstancesForWorldServer>>(responseContentString);
            } 
            else 
            {
                output = new List<GetZoneInstancesForWorldServer>();
            }

            return output;
        }
    }
}
