using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Users
{
    public class GetUserSessionRequest : IRequestHandler<GetUserSessionRequest, IActionResult>, IRequest
    {
        public Guid UserSessionGUID { get; set; }

        private GetUserSession output;
        private Guid customerGUID;
        private IUsersRepository usersRepository;

        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            output = await usersRepository.GetUserSession(customerGUID, UserSessionGUID);

            return new OkObjectResult(output);
        }
    }

    /*public class GetUserSessionRequest : IRequestHandler<GetUserSessionRequest, IActionResult>, IRequest
    {
        public Guid UserSessionGUID { get; set; }

        private Models.StoredProcs.GetUserSession Output;
        private Guid CustomerGUID;
        private IUsersRepository usersRepository;

        public GetUserSessionRequest(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            CustomerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            //Output = await usersRepository.GetUserSession(CustomerGUID, UserSessionGUID);
            Output = new Models.StoredProcs.GetUserSession();

            return new OkObjectResult(Output);
        }
    }*/


}
