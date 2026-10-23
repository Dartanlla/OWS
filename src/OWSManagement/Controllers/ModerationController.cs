using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSManagement.Auth;

namespace OWSManagement.Controllers
{
    /// <summary>Player reports, sanctions across all accounts, the staff action log and chat search.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ModerationController : Controller
    {
        private readonly ISanctionsRepository _sanctions;
        private readonly IConsoleQueriesRepository _queries;
        private readonly IChatLogRepository _chatLog;
        private readonly IPlayerReportsRepository _reports;
        private readonly ConsoleCustomer _customer;

        public ModerationController(ISanctionsRepository sanctions, IConsoleQueriesRepository queries, IChatLogRepository chatLog,
            IPlayerReportsRepository reports, ConsoleCustomer customer)
        {
            _reports = reports;
            _sanctions = sanctions;
            _queries = queries;
            _chatLog = chatLog;
            _customer = customer;
        }

        [HttpGet]
        [Route("Sanctions")]
        public async Task<IEnumerable<Sanction>> Sanctions([FromQuery] bool active = false)
        {
            return await _sanctions.GetRecent(_customer.CustomerGUID, active, 200);
        }

        [HttpGet]
        [Route("StaffActions")]
        public async Task<IEnumerable<AuditEntry>> StaffActions([FromQuery] string search)
        {
            return await _queries.GetAudit(_customer.CustomerGUID, "zone", "staff_action", search, 200);
        }

        [HttpGet]
        [Route("Reports")]
        public async Task<IEnumerable<PlayerReport>> Reports([FromQuery] bool open = false)
        {
            return await _reports.List(_customer.CustomerGUID, open, 200);
        }

        public class ResolveInput
        {
            public string Note { get; set; }
        }

        [HttpPost]
        [Route("Reports/{reportId:long}/Resolve")]
        public async Task<IActionResult> Resolve(long reportId, [FromBody] ResolveInput input)
        {
            bool resolved = await _reports.Resolve(_customer.CustomerGUID, reportId, ConsoleAuth.Actor(User), input?.Note);
            return Ok(new SuccessAndErrorMessage { Success = resolved, ErrorMessage = resolved ? "" : "The report is already resolved." });
        }

        [HttpGet]
        [Route("Chat")]
        public async Task<IEnumerable<ChatLogEntry>> Chat([FromQuery] string sender, [FromQuery] string text, [FromQuery] long before = 0)
        {
            return await _chatLog.Search(_customer.CustomerGUID, sender, text, before, 200);
        }
    }
}
