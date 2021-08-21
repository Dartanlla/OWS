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
    public class GetPlayerGroupsCharacterIsInRequest : IRequestHandler<GetPlayerGroupsCharacterIsInRequest, IActionResult>, IRequest
    {
        public Guid UserSessionGUID { get; set; }
        public string CharacterName { get; set; }
        public int PlayerGroupTypeID { get; set; }

        private IEnumerable<GetPlayerGroupsCharacterIsIn> output;
        private Guid customerGUID;
        private IUsersRepository usersRepository;

        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            output = await usersRepository.GetPlayerGroupsCharacterIsIn(customerGUID, UserSessionGUID, CharacterName, PlayerGroupTypeID);

            return new OkObjectResult(output);
        }
    }
}
