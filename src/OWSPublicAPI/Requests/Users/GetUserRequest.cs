using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Users
{
    public class GetUserRequest : IRequestHandler<GetUserRequest, IActionResult>, IRequest
    {

        private User output;
        private Guid customerGuid;
        private Guid userGuid;
        private IUsersRepository usersRepository;

        public void SetData(Guid id, IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            userGuid = id;
            this.customerGuid = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            output = await usersRepository.GetUser(customerGuid, userGuid);

            return new OkObjectResult(output);
        }
    }
}
