using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSCharacterPersistence.Controllers
{
    /// <summary>
    /// Player reports, filed by the zone server for a player's /report. The zone checks the reporter and limits how
    /// often one player reports. Service key only.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : Controller
    {
        private readonly IPlayerReportsRepository _reports;
        private readonly IHeaderCustomerGUID _customerGuid;

        public ReportsController(IPlayerReportsRepository reports, IHeaderCustomerGUID customerGuid)
        {
            _reports = reports;
            _customerGuid = customerGuid;
        }

        public class AddRequest
        {
            public string ReporterName { get; set; }
            public string TargetName { get; set; }
            public string Reason { get; set; }
            public string MapName { get; set; }
            public int MapInstanceID { get; set; }
            /// <summary>Positions of both players, when the target is on the same zone.</summary>
            public JsonElement Details { get; set; }
        }

        [HttpPost]
        [Route("Add")]
        public async Task<IActionResult> Add([FromBody] AddRequest request)
        {
            if (!SamsaraServiceKey.IsValid(Request)) return Unauthorized();
            if (request == null) return BadRequest();

            (long reportID, string error) = await _reports.Add(_customerGuid.CustomerGUID, new NewPlayerReport
            {
                ReporterName = request.ReporterName,
                TargetName = request.TargetName,
                Reason = request.Reason,
                MapName = request.MapName,
                MapInstanceID = request.MapInstanceID > 0 ? request.MapInstanceID : null,
                Details = request.Details.ValueKind == JsonValueKind.Object ? request.Details.GetRawText() : "{}"
            });
            return Ok(new { Success = error == null, ErrorMessage = error ?? "", ReportID = reportID });
        }
    }
}
