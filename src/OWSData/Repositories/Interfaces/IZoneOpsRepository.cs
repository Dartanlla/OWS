using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSData.Repositories.Interfaces
{
    /// <summary>
    /// ZoneCommands and GameEvents (Postgres only): what the staff console sends to zone servers, and the game events
    /// zone servers send back, both carried by CharacterPersistence api/ZoneSync.
    /// </summary>
    public interface IZoneOpsRepository
    {
        /// <summary>
        /// Commands for this instance, and for characters online on it (CharOnMapInstance), that are not finished or
        /// expired. Marks them delivered; a command delivered to the same instance comes back after 60 s without a result.
        /// </summary>
        Task<IEnumerable<ZoneCommand>> TakePendingCommands(Guid customerGUID, int mapInstanceID);

        /// <summary>Records results. "retry" (the character is not on this zone) leaves the command open for its next zone.</summary>
        Task CompleteCommands(Guid customerGUID, IEnumerable<ZoneCommandResult> results);

        Task<long> AddCommand(Guid customerGUID, int? mapInstanceID, string characterName, string command, string args, string createdBy, TimeSpan lifetime);

        /// <summary>One command per running instance. Returns how many were queued.</summary>
        Task<int> AddCommandForAllInstances(Guid customerGUID, string command, string args, string createdBy, TimeSpan lifetime);

        Task<IEnumerable<ZoneCommand>> GetRecentCommands(Guid customerGUID, string characterName, int limit);

        Task<int> AddEvents(Guid customerGUID, int mapInstanceID, IReadOnlyCollection<GameEventIn> events);

        /// <summary>Newest first. Empty characterName or eventName means any; beforeID pages backwards.</summary>
        Task<IEnumerable<GameEventRow>> GetEvents(Guid customerGUID, string characterName, string eventName, long beforeID, int limit);
    }
}
