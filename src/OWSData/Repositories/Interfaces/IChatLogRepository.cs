using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSData.Repositories.Interfaces
{
    /// <summary>ChatLog (Postgres only): what OWSChat relayed, and what a mute dropped.</summary>
    public interface IChatLogRepository
    {
        Task AddBatch(IReadOnlyCollection<ChatLogEntry> entries);

        /// <summary>Newest first. Empty sender or text means any; beforeID pages backwards.</summary>
        Task<IEnumerable<ChatLogEntry>> Search(Guid customerGUID, string sender, string text, long beforeID, int limit);
    }
}
