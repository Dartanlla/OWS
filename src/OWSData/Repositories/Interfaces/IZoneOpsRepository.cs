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
        /// expired. Marks them delivered; a command other than a gift comes back after 60 s without a result. A gift is
        /// delivered at most once, so a zone that stops before reporting can never grant it a second time.
        /// </summary>
        Task<IEnumerable<ZoneCommand>> TakePendingCommands(Guid customerGUID, int mapInstanceID);

        /// <summary>
        /// Records results. "retry" (the character is not on this zone, or not ready) releases the command for the next
        /// poll, only from the zone that holds it and only for the delivery attempt that zone got.
        /// </summary>
        Task CompleteCommands(Guid customerGUID, int mapInstanceID, IEnumerable<ZoneCommandResult> results);

        Task<long> AddCommand(Guid customerGUID, int? mapInstanceID, string characterName, string command, string args, string createdBy, TimeSpan lifetime);

        /// <summary>One command per running instance. Returns how many were queued.</summary>
        Task<int> AddCommandForAllInstances(Guid customerGUID, string command, string args, string createdBy, TimeSpan lifetime);

        Task<IEnumerable<ZoneCommand>> GetRecentCommands(Guid customerGUID, string characterName, int limit);

        Task<int> AddEvents(Guid customerGUID, int mapInstanceID, IReadOnlyCollection<GameEventIn> events);

        /// <summary>Newest first. Empty characterName or eventName means any; beforeID pages backwards.</summary>
        Task<IEnumerable<GameEventRow>> GetEvents(Guid customerGUID, string characterName, string eventName, long beforeID, int limit);
        /// <summary>Replaces the instance's players' positions with these; players no longer sent are removed.</summary>
        Task SetPositions(Guid customerGUID, int mapInstanceID, string mapName, IReadOnlyCollection<LivePositionIn> positions);
        /// <summary>Positions sent in the last 30 s. Empty mapName means every map.</summary>
        Task<IEnumerable<LivePosition>> GetPositions(Guid customerGUID, string mapName);
    }
}
