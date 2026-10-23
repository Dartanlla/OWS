using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSManagement.Auth;

namespace OWSManagement.Controllers
{
    /// <summary>Sanctions across all accounts, the staff action log and chat search.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ModerationController : Controller
    {
        private readonly ISanctionsRepository _sanctions;
        private readonly IConsoleQueriesRepository _queries;
        private readonly IChatLogRepository _chatLog;
        private readonly ConsoleCustomer _customer;

        public ModerationController(ISanctionsRepository sanctions, IConsoleQueriesRepository queries, IChatLogRepository chatLog, ConsoleCustomer customer)
        {
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
        [Route("Chat")]
        public async Task<IEnumerable<ChatLogEntry>> Chat([FromQuery] string sender, [FromQuery] string text, [FromQuery] long before = 0)
        {
            return await _chatLog.Search(_customer.CustomerGUID, sender, text, before, 200);
        }
    }
}
