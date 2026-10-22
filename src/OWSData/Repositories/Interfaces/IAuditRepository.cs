using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSData.Repositories.Interfaces
{
    /// <summary>
    /// The AuditLog table (Postgres only). Add never throws: an audit write that fails is logged and the caller's
    /// operation goes on.
    /// </summary>
    public interface IAuditRepository
    {
        Task Add(Guid customerGUID, string source, string actor, string action, string target, object detail, string clientIP = null);
        Task<IEnumerable<AuditEntry>> GetRecent(Guid customerGUID, int limit, string search);
    }
}
