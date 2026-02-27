using System;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;

namespace OWSData.Repositories.Interfaces
{
    public interface IUserSessionRepository
    {
        Task<GetUserSession> GetUserSession(Guid UserGuid);
        Task SetUserSession(Guid UserGuid, GetUserSession userSession);
    }
}
