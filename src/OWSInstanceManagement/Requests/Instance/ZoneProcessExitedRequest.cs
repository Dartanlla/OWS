using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using OWSShared.RequestPayloads;
using Serilog;
using System;
using System.Threading.Tasks;

namespace OWSInstanceManagement.Requests.Instance
{
    /// <summary>
    /// The instance launcher reports that a zone server process it started has exited. Logs
    /// zone_process_exited, which the Grafana alert reads when Expected is false.
    /// </summary>
    public class ZoneProcessExitedRequest
    {
        public ZoneProcessExitedRequestPayload request { get; set; }

        private IInstanceManagementRepository _instanceManagementRepository;
        private Guid _customerGUID;

        public void SetData(IInstanceManagementRepository instanceManagementRepository, IHeaderCustomerGUID customerGuid)
        {
            _instanceManagementRepository = instanceManagementRepository;
            _customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<IActionResult> Handle()
        {
            GetServerInstanceFromPort instance = await _instanceManagementRepository.GetZoneInstance(_customerGUID, request.ZoneInstanceID);
            bool expected = IsExpectedExit(instance);

            const string template = "zone_process_exited {ZoneInstanceId} {MapName}:{Port} exit code {ExitCode}, expected {Expected}";
            if (expected)
            {
                Log.Information(template, request.ZoneInstanceID, request.MapName, request.Port, request.ExitCode, expected);
            }
            else
            {
                Log.Warning(template, request.ZoneInstanceID, request.MapName, request.Port, request.ExitCode, expected);
            }

            return new OkObjectResult(new SuccessAndErrorMessage { Success = true, ErrorMessage = "" });
        }

        // Every deliberate stop marks the row first: ShutDownServerInstance sets status 3, and a launcher shutdown
        // or orphan cleanup deletes it (GetZoneInstance then returns an empty row).
        public static bool IsExpectedExit(GetServerInstanceFromPort instance)
        {
            return instance.MapInstanceID == 0 || instance.Status == 3;
        }
    }
}
