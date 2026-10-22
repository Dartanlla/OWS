using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSCharacterPersistence.Controllers
{
    /// <summary>
    /// Zone servers record staff actions and player trades in AuditLog. Service key only: players cannot reach this
    /// service, and the key keeps a stray caller from writing audit rows.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuditController : Controller
    {
        private readonly IAuditRepository _auditRepository;
        private readonly IHeaderCustomerGUID _customerGuid;

        public AuditController(IAuditRepository auditRepository, IHeaderCustomerGUID customerGuid)
        {
            _auditRepository = auditRepository;
            _customerGuid = customerGuid;
        }

        public class AddAuditRequest
        {
            public string Actor { get; set; }
            public string Action { get; set; }
            public string Target { get; set; }
            public JsonElement? Detail { get; set; }
        }

        [HttpPost]
        [Route("Add")]
        public async Task<IActionResult> Add([FromBody] AddAuditRequest request)
        {
            if (!SamsaraServiceKey.IsValid(Request)) return Unauthorized();
            if (string.IsNullOrWhiteSpace(request?.Action)) return BadRequest();

            await _auditRepository.Add(_customerGuid.CustomerGUID, "zone", request.Actor, request.Action, request.Target,
                request.Detail?.GetRawText());
            return Ok();
        }
    }
}
