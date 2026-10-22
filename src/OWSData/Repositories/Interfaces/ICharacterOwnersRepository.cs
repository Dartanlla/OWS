using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OWSData.Models.Composites;

namespace OWSData.Repositories.Interfaces
{
    /// <summary>
    /// One owning session per character (Postgres only): the zone server claims a character with a join ticket and
    /// gets the token its saves must carry.
    /// </summary>
    public interface ICharacterOwnersRepository
    {
        Task<ClaimCharacterResult> ClaimWithTicket(Guid customerGUID, Guid ticketGUID, int mapInstanceID);
        Task<ClaimCharacterResult> ClaimByName(Guid customerGUID, string characterName, int mapInstanceID);
        Task<CancelJoinTicketResult> CancelTicket(Guid customerGUID, Guid ticketGUID);
        Task<List<StaleCharacterOwner>> GetStaleOwners(Guid customerGUID, IEnumerable<CharacterOwnerCheck> owners);
        // True when the character has an owner and ownerToken is not it. A character with no owner row is not stale.
        Task<bool> IsStaleOwnerWrite(Guid customerGUID, string characterName, Guid? ownerToken);
        Task<bool> IsStaleOwnerWrite(Guid customerGUID, int characterID, Guid? ownerToken);
    }
}
