using OWSData.Models.Composites;
using OWSData.Repositories.Implementations.MSSQL;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.DTOs;
using OWSShared.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace OWSPublicAPI.Requests.Users
{
    public class LogoutRequest
    {
        private readonly LogoutDTO _logoutDTO;
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly IUsersRepository _usersRepository;
        private readonly IAuditRepository _auditRepository;

        public LogoutRequest(LogoutDTO logoutDTO, IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid, IAuditRepository auditRepository)
        {
            _logoutDTO = logoutDTO;
            _customerGuid = customerGuid;
            _usersRepository = usersRepository;
            _auditRepository = auditRepository;
        }

        public async Task<SuccessAndErrorMessage> Handle()
        {
            Log.Information("user_logout session {UserSessionGuid}", _logoutDTO.UserSessionGUID);
            // Read before the logout deletes the session row, so the audit names the account and character.
            var session = await _usersRepository.GetUserSession(_customerGuid.CustomerGUID, _logoutDTO.UserSessionGUID);
            await _auditRepository.Add(_customerGuid.CustomerGUID, "publicapi", session?.FirstName ?? "unknown", "user_logout",
                session?.SelectedCharacterName, new { userSessionGuid = _logoutDTO.UserSessionGUID });
            return await _usersRepository.Logout(_customerGuid.CustomerGUID, _logoutDTO.UserSessionGUID);
        }
    }
}
