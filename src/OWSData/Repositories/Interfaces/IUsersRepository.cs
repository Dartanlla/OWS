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
        Task<CreateCharacter> CreateSamsaraCharacter(Guid customerGUID, Guid userSessionGUID, string characterName, string className, string initialPersistentData = null);
        Task<SuccessAndErrorMessage> CreateCharacterUsingDefaultCharacterValues(Guid customerGUID, Guid userGUID, string characterName, string defaultSetName);
        Task<IEnumerable<GetAllCharacters>> GetAllCharacters(Guid customerGUID, Guid userSessionGUID);
        Task<IEnumerable<GetAllSamsaraCharacters>> GetAllSamsaraCharacters(Guid customerGUID, Guid userSessionGUID);
        Task<User> GetUser(Guid customerGuid, Guid userGuid);
        Task<IEnumerable<User>> GetUsers(Guid customerGuid);
        // Management-console search, capped at 200 rows. Empty text returns the first page.
        Task<IEnumerable<AdminUserSummary>> SearchUsers(Guid customerGuid, string searchText);
        // Sets Users.IsInternalNetworkTestUser, the account-level LAN test flag.
        Task<SuccessAndErrorMessage> SetNetworkTestFlagForUser(Guid customerGuid, Guid userGuid, bool isInternalNetworkTestUser);
        Task<GetUserSession> GetUserSession(Guid customerGUID, Guid userSessionGUID);
        Task<GetUserSession> GetUserSessionORM(Guid customerGUID, Guid userSessionGUID);
        Task<GetUserSessionComposite> GetUserSessionParallel(Guid customerGUID, Guid userSessionGUID);
        Task<PlayerLoginAndCreateSession> LoginAndCreateSession(Guid customerGUID, string email, string password, bool dontCheckPassword = false);
        Task<PlayerLoginAndCreateSession> SteamLoginAndCreateSession(Guid customerGUID, string steamId, string personaName);
        Task<SuccessAndErrorMessage> Logout(Guid customerGuid, Guid userSessionGuid);
        Task<SuccessAndErrorMessage> UserSessionSetSelectedCharacter(Guid customerGUID, Guid userSessionGUID, string selectedCharacterName);
        Task<SuccessAndErrorMessage> RegisterUser(Guid customerGUID, string userName, string password, string firstName, string lastName, string role = null);
        // Management-console only. role must already be normalised via UserRoles.TryNormalize;
        // Users.Role is VARCHAR(10) and nothing downstream re-validates it.
        Task<SuccessAndErrorMessage> UpdateUserRole(Guid customerGuid, Guid userGuid, string role);
        // Region routing. region must already be validated against GameServersConfig by the caller:
        // the routing queries compare WorldServers.ServerRegion to this value with exact equality.
        Task<SuccessAndErrorMessage> SetPreferredServerRegion(Guid customerGuid, Guid userGuid, string region);
        Task<GetUserSession> GetUserFromEmail(Guid customerGUID, string email);
        Task<SuccessAndErrorMessage> RemoveCharacter(Guid customerGUID, Guid userSessionGUID, string characterName);
        Task<SuccessAndErrorMessage> UpdateUser(Guid customerGuid, Guid userGuid, string firstName, string lastName, string email);
        
    }
}
