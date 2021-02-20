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
    public class GetPlayerGroupsCharacterIsInRequest
    {
        public Guid UserSessionGUID { get; set; }
        public string CharacterName { get; set; }
        public int PlayerGroupTypeID { get; set; }

        private IEnumerable<GetPlayerGroupsCharacterIsIn> Output;
        private Guid CustomerGUID;
        private IUsersRepository usersRepository;

        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            CustomerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        public async Task<IActionResult> Run()
        {
            Output = await usersRepository.GetPlayerGroupsCharacterIsIn(CustomerGUID, UserSessionGUID, CharacterName, PlayerGroupTypeID);

            return new OkObjectResult(Output);
        }
    }
}
