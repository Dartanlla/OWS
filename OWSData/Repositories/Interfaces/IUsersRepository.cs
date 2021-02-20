using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;

namespace OWSData.Repositories.Interfaces
{
    public interface IUsersRepository
    {
        Task<CreateCharacter> CreateCharacter(Guid customerGUID, Guid userSessionGUID, string characterName, string className);
        Task<IEnumerable<GetPlayerGroupsCharacterIsIn>> GetPlayerGroupsCharacterIsIn(Guid customerGUID, Guid userSessionGUID, string characterName, int playerGroupTypeID = 0);
        Task<GetUserSession> GetUserSession(Guid customerGUID, Guid userSessionGUID);
        Task<GetUserSession> GetUserSessionORM(Guid customerGUID, Guid userSessionGUID);
        Task<GetUserSessionComposite> GetUserSessionParallel(Guid customerGUID, Guid userSessionGUID);
        Task<PlayerLoginAndCreateSession> LoginAndCreateSession(Guid customerGUID, string email, string password, bool dontCheckPassword = false);
        Task<SuccessAndErrorMessage> UserSessionSetSelectedCharacter(Guid customerGUID, Guid userSessionGUID, string selectedCharacterName);
        Task<SuccessAndErrorMessage> RegisterUser(Guid customerGUID, string userName, string password, string firstName, string lastName);
        Task<GetUserSession> GetUserFromEmail(Guid customerGUID, string email);

        
    }
}
