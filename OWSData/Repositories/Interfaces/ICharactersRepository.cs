using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;

namespace OWSData.Repositories.Interfaces
{
    public interface ICharactersRepository
    {
        Task<GetCharByCharName> GetCharByCharName(Guid customerGUID, string characterName);
        Task<JoinMapByCharName> JoinMapByCharName(Guid customerGUID, string characterName, string zoneName, int playerGroupType);
        Task<CheckMapInstanceStatus> CheckMapInstanceStatus(Guid customerGUID, int mapInstanceID);
        Task AddCharacterToMapInstanceByCharName(Guid customerGUID, string characterName, int mapInstanceID);
    }
}
