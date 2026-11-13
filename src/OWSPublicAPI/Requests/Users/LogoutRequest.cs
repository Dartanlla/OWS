using Microsoft.AspNetCore.Mvc;
﻿using OWSData.Models.Composites;
using OWSData.Repositories.Implementations.MSSQL;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.DTOs;
using OWSShared.Interfaces;
using System;
using System.Threading.Tasks;

namespace OWSPublicAPI.Requests.Users
{
    public class LogoutRequest
    {
        private readonly LogoutDTO _logoutDTO;
        private readonly IHeaderCustomerGUID _customerGuid;
        private readonly IUsersRepository _usersRepository;

        public LogoutRequest(LogoutDTO logoutDTO, IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            _logoutDTO = logoutDTO;
            _customerGuid = customerGuid;
            _usersRepository = usersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            if (await PlayerAuthorization.GetSessionAsync(_usersRepository, _customerGuid.CustomerGUID, _logoutDTO.UserSessionGUID) == null)
                return new UnauthorizedResult();

            return new OkObjectResult(await _usersRepository.Logout(_customerGuid.CustomerGUID, _logoutDTO.UserSessionGUID));
        }
    }
}
