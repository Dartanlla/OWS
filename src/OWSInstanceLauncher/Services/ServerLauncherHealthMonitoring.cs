using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OWSData.Models;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.RequestPayloads;
using OWSShared.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Serilog;

namespace OWSInstanceLauncher.Services
{
    public class ServerLauncherHealthMonitoring : IServerHealthMonitoringJob
    {
        private readonly IOptions<OWSInstanceLauncherOptions> _OWSInstanceLauncherOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IZoneServerProcessesRepository _zoneServerProcessesRepository;
        private readonly IOWSInstanceLauncherDataRepository _owsInstanceLauncherDataRepository;

        public ServerLauncherHealthMonitoring(IOptions<OWSInstanceLauncherOptions> OWSInstanceLauncherOptions, IHttpClientFactory httpClientFactory, IZoneServerProcessesRepository zoneServerProcessesRepository,
            IOWSInstanceLauncherDataRepository owsInstanceLauncherDataRepository)
        {
            _OWSInstanceLauncherOptions = OWSInstanceLauncherOptions;
            _httpClientFactory = httpClientFactory;
            _zoneServerProcessesRepository = zoneServerProcessesRepository;
            _owsInstanceLauncherDataRepository = owsInstanceLauncherDataRepository;
        }

        public void DoWork()
        {
            Log.Information("Server Health Monitoring is checking for broken Zone Server Instances...");

            int worldServerID = _owsInstanceLauncherDataRepository.GetWorldServerID();

            if (worldServerID < 1)
            {
                Log.Warning("Server Health Monitoring is waiting for a valid World Server ID...");
                return;
            }

            Log.Information("Server Health Monitoring is getting a list of Zone Server Instances...");

            //Get a list of ZoneInstances from api/Instance/GetZoneInstancesForWorldServer
            List<GetZoneInstancesForWorldServer> zoneInstances = GetZoneInstancesForWorldServer(worldServerID);

            foreach (var zoneInstance in zoneInstances)
            {
                if (zoneInstance.LastServerEmptyDate < DateTime.Now.AddMinutes(0 - zoneInstance.MinutesToShutdownAfterEmpty))
                {
                    //Shut down Zone Server Instance

                }
            }
        }

        public void Dispose()
        {
            Log.Information("Shutting Down OWS Server Health Monitoring...");
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

            var getZoneInstancesForWorldServerRequest = new StringContent(JsonSerializer.Serialize(worldServerIDRequestPayload), Encoding.UTF8, "application/json");

            var responseMessageTask = instanceManagementHttpClient.PostAsync("api/Instance/GetZoneInstancesForWorldServer", getZoneInstancesForWorldServerRequest);
            var responseMessage = responseMessageTask.Result;

            if (responseMessage.IsSuccessStatusCode)
            {
                var responseContentAsync = responseMessage.Content.ReadAsStringAsync();
                string responseContentString = responseContentAsync.Result;
                output = JsonSerializer.Deserialize<List<GetZoneInstancesForWorldServer>>(responseContentString);
            }
            else
            {
                output = new List<GetZoneInstancesForWorldServer>();
            }

            return output;
        }
    }
}
