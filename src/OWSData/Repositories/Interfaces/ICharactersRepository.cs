using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GuildServiceApp.Protos;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using PartyServiceApp.Protos;

namespace OWSData.Repositories.Interfaces
{
    public interface ICharactersRepository
    {
        Task AddCharacterToMapInstanceByCharName(Guid customerGUID, string characterName, int mapInstanceID);
        Task<MapInstances> CheckMapInstanceStatus(Guid customerGUID, int mapInstanceID);
        Task CleanUpInstances(Guid customerGUID);
        Task<CharacterSaveData> GetSaveDataByCharName(Guid customerGUID, string characterName);
        Task<IEnumerable<GetCharStatsByCharName>> GetCharStatsByCharName(Guid customerGUID, string characterName);
        Task<IEnumerable<GetCharQuestsByCharName>> GetCharQuetsByCharName(Guid customerGUID, string characterName);
        Task<IEnumerable<GetCharInventoryByCharName>> GetCharInventoryByCharName(Guid customerGUID, string characterName);
        Task<GetCharByCharName> GetCharByCharName(Guid customerGUID, string characterName);
        Task<JoinMapByCharName> JoinMapByCharName(Guid customerGUID, string characterName, string zoneName);
        Task UpdateCharacterStats(Guid customerGUID, string characterName, IEnumerable<UpdateCharacterStats> updateCharacterStats);
        Task UpdateCharacterQuests(Guid customerGUID, string characterName, IEnumerable<UpdateCharacterQuest> updateCharacterQuests);
        Task UpdateCharacterInventory(Guid customerGUID, string characterName, IEnumerable<UpdateCharacterInventory> updateCharacterInventory);
        Task UpdatePosition(Guid customerGUID, string characterName, string mapName, float X, float Y, float Z, float RX, float RY, float RZ);
        Task PlayerLogout(Guid customerGUID, string characterName);
        Task<MapInstances> SpinUpInstance(Guid customerGUID, string zoneName, int playerGroupId = 0);
        Task AddQuestListToDatabase(Guid customerGUID, IEnumerable<AddQuestListToDabase> addQuestListToDabase);
        Task<IEnumerable<GetQuestsFromDb>> GetQuestsFromDatabase(Guid customerGUID);

        Task<PartyToSend> CreatePartyOrAddMember(Guid customerGUID, PartyToSend partyRequest);
        Task<PartyToSend> GetInitialPartySettings(Guid customerGUID, string charName);

        Task<PartyToSend> LeaveParty(Guid customerGUID, PartyToSend partyRequest);

        Task<PartyToSend> ChangePartyLeader(Guid customerGUID, PartyToSend partyRequest);

        Task<GuildToSend> CreateGuildOrAddMember(Guid customerGUID, GuildToSend guildRequest);

        Task<GuildToSend> GetInitialGuildSettings(Guid customerGUID, string charName);

        Task<GuildToSend> AddGuildAbilities(Guid customerGUID, GuildToSend guildInfo);


    }
}
