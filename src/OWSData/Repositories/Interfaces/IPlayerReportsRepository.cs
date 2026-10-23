using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSData.Repositories.Interfaces
{
    /// <summary>
    /// PlayerReports (Postgres only): players' reports of other players. Zone servers file them through
    /// CharacterPersistence api/Reports; the staff console reads and resolves them.
    /// </summary>
    public interface IPlayerReportsRepository
    {
        /// <summary>
        /// Files a report with a copy of both players' chat from the last 10 minutes. Error is "unknown_character" when
        /// either name has no character, or "too_many" when the reporter filed 20 reports in the last 24 h.
        /// </summary>
        Task<(long ReportID, string Error)> Add(Guid customerGUID, NewPlayerReport report);
        /// <summary>Open reports first, then newest first.</summary>
        Task<IEnumerable<PlayerReport>> List(Guid customerGUID, bool openOnly, int limit);
        /// <summary>Reports by or against the character, newest first.</summary>
        Task<IEnumerable<PlayerReport>> ForCharacter(Guid customerGUID, string characterName, int limit);
        Task<bool> Resolve(Guid customerGUID, long reportID, string resolvedBy, string note);
        Task<int> CountOpen(Guid customerGUID);
    }
}
