using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;

namespace OWSData.Repositories.Interfaces
{
    public interface IUsersRepository
    {
        Task<CreateCharacter> CreateCharacter(Guid customerGUID, Guid userSessionGUID, string characterName, string className);
        Task<SuccessAndErrorMessage> CreateCharacterUsingDefaultCharacterValues(Guid customerGUID, Guid userGUID, string characterName, string defaultSetName);
        Task<IEnumerable<GetAllCharacters>> GetAllCharacters(Guid customerGUID, Guid userSessionGUID);
        Task<IEnumerable<GetPlayerGroupsCharacterIsIn>> GetPlayerGroupsCharacterIsIn(Guid customerGUID, Guid userSessionGUID, string characterName, int playerGroupTypeID = 0);
        Task<User> GetUser(Guid customerGuid, Guid userGuid);
        Task<IEnumerable<User>> GetUsers(Guid customerGuid);
        Task<GetUserSession> GetUserSession(Guid customerGUID, Guid userSessionGUID);
        Task<GetUserSession> GetUserSessionORM(Guid customerGUID, Guid userSessionGUID);
        Task<GetUserSessionComposite> GetUserSessionParallel(Guid customerGUID, Guid userSessionGUID);
        Task<PlayerLoginAndCreateSession> LoginAndCreateSession(Guid customerGUID, string email, string password, bool dontCheckPassword = false);
        Task<SuccessAndErrorMessage> Logout(Guid customerGuid, Guid userSessionGuid);
        Task<SuccessAndErrorMessage> UserSessionSetSelectedCharacter(Guid customerGUID, Guid userSessionGUID, string selectedCharacterName);
        Task<SuccessAndErrorMessage> RegisterUser(Guid customerGUID, string userName, string password, string firstName, string lastName);
        Task<GetUserSession> GetUserFromEmail(Guid customerGUID, string email);
        Task<SuccessAndErrorMessage> RemoveCharacter(Guid customerGUID, Guid userSessionGUID, string characterName);
        Task<SuccessAndErrorMessage> UpdateUser(Guid customerGuid, Guid userGuid, string firstName, string lastName, string email);
        Task<SuccessAndErrorMessage> RemoveUser(Guid customerGuid, Guid userGuid, string email);

    }
}
