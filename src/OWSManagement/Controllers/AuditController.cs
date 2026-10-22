using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Controllers
{
    /// <summary>
    /// The AuditLog for the management console: logins, zone joins, admin console changes, staff actions and trades.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuditController : Controller
    {
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly IAuditRepository _auditRepository;

        public AuditController(IHeaderCustomerGUID customerGuid, IAuditRepository auditRepository)
        {
            _customerGuid = customerGuid;
            _auditRepository = auditRepository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_customerGuid.CustomerGUID == Guid.Empty)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        /// <summary>
        /// List Audit Entries
        /// </summary>
        /// <remarks>
        /// Newest first. search matches the actor, action or target (case-insensitive substring); limit is 1-1000.
        /// </remarks>
        [HttpGet]
        [Route("")]
        [Produces(typeof(IEnumerable<AuditEntry>))]
        public async Task<IEnumerable<AuditEntry>> Get([FromQuery] string search, [FromQuery] int limit = 200)
        {
            return await _auditRepository.GetRecent(_customerGuid.CustomerGUID, limit, search);
        }
    }
}
