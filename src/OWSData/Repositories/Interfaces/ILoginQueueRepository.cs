using System;
using System.Threading.Tasks;

namespace OWSData.Repositories.Interfaces
{
    /// <summary>
    /// LoginQueue (Postgres only): the line to enter the world while the console's player cap is reached.
    /// </summary>
    public interface ILoginQueueRepository
    {
        /// <summary>
        /// 0 when the session may enter the world now, else its place in line (1 is next). Players in the world, and
        /// sessions let in during the last 2 minutes that have not arrived yet, count against the cap. A character that
        /// is still listed in the world, or left it in the last 90 s, enters at once. Callers skip staff.
        /// </summary>
        Task<int> Check(Guid customerGUID, Guid userSessionGUID, Guid userGUID, string characterName, int cap);
        /// <summary>Sessions asked for in the last 30 s that are still waiting.</summary>
        Task<int> Waiting(Guid customerGUID);
    }
}
