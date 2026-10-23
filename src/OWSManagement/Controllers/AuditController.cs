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
        private readonly IConsoleQueriesRepository _queries;

        public AuditController(IHeaderCustomerGUID customerGuid, IConsoleQueriesRepository queries)
        {
            _customerGuid = customerGuid;
            _queries = queries;
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
        /// Newest first. source and action match exactly when given; search matches the actor, target or detail
        /// (case-insensitive substring); limit is 1-1000.
        /// </remarks>
        [HttpGet]
        [Route("")]
        [Produces(typeof(IEnumerable<AuditEntry>))]
        public async Task<IEnumerable<AuditEntry>> Get([FromQuery] string search, [FromQuery] string source, [FromQuery] string action,
            [FromQuery] int limit = 200)
        {
            return await _queries.GetAudit(_customerGuid.CustomerGUID, source, action, search, limit);
        }
    }
}
