using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;

namespace OWSData.Repositories.Interfaces
{
    public interface ICharactersRepository
    {
        Task AddCharacterToMapInstanceByCharName(Guid customerGUID, string characterName, int mapInstanceID);
        Task AddOrUpdateCustomCharacterData(Guid customerGUID, AddOrUpdateCustomCharacterData addOrUpdateCustomCharacterData);
        Task<MapInstances> CheckMapInstanceStatus(Guid customerGUID, int mapInstanceID);
        Task CleanUpInstances(Guid customerGUID);
        Task<IEnumerable<GetCharStatsByCharName>> GetCharStatsByCharName(Guid customerGUID, string characterName);
        Task<IEnumerable<GetCharInventoryByCharName>> GetCharInventoryByCharName(Guid customerGUID, string characterName);
        Task<GetCharByCharName> GetCharByCharName(Guid customerGUID, string characterName);
        Task<IEnumerable<CustomCharacterData>> GetCustomCharacterData(Guid customerGUID, string characterName);
        Task<JoinMapByCharName> JoinMapByCharName(Guid customerGUID, string characterName, string zoneName, int playerGroupType);
        Task UpdateCharacterStats(Guid customerGUID, string characterName, IEnumerable<UpdateCharacterStats> updateCharacterStats);
        Task UpdateCharacterAbilities(Guid customerGUID, string characterName, IEnumerable<UpdateCharacterAbilities> CharacterAbilities);
        Task UpdateCharacterInventory(Guid customerGUID, string characterName, IEnumerable<UpdateCharacterInventory> updateCharacterInventory);
        Task UpdatePosition(Guid customerGUID, string characterName, string mapName, float X, float Y, float Z, float RX, float RY, float RZ);
        Task PlayerLogout(Guid customerGUID, string characterName);
        Task AddAbilityToCharacter(Guid customerGUID, string abilityName, string characterName, int abilityLevel, string charHasAbilitiesCustomJSON);
        Task<IEnumerable<Abilities>> GetAbilities(Guid customerGUID);
        Task<IEnumerable<GetCharacterAbilities>> GetCharacterAbilities(Guid customerGUID, string characterName);
        Task<IEnumerable<GetAbilityBars>> GetAbilityBars(Guid customerGUID, string characterName);
        Task<IEnumerable<GetAbilityBarsAndAbilities>> GetAbilityBarsAndAbilities(Guid customerGUID, string characterName);
        Task<MapInstances> SpinUpInstance(Guid customerGUID, string zoneName, int playerGroupId = 0);
        Task RemoveAbilityFromCharacter(Guid customerGUID, string abilityName, string characterName);
        
    }
}
