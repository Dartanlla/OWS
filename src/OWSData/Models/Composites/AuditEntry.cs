using System;

namespace OWSData.Models.Composites
{
    /// <summary>
    /// One AuditLog row: who (Actor) did what (Action) to what (Target), from where (Source: publicapi, management,
    /// zone). Detail is a JSON object whose shape depends on Action. ClientIP is set for admin console edits only.
    /// </summary>
    public class AuditEntry
    {
        public long AuditLogID { get; set; }
        public DateTime CreateDate { get; set; }
        public string Source { get; set; }
        public string Actor { get; set; }
        public string Action { get; set; }
        public string Target { get; set; }
        public string Detail { get; set; }
        public string ClientIP { get; set; }
    }
}
