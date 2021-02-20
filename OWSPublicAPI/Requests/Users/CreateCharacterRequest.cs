using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Users
{
    public class CreateCharacterRequest
    {
        public Guid UserSessionGUID { get; set; }
        public string CharacterName { get; set; }
        public string ClassName { get; set; }

        private CreateCharacter Output;
        private Guid CustomerGUID;
        private IUsersRepository usersRepository;

        public void SetData(IUsersRepository usersRepository, IHeaderCustomerGUID customerGuid)
        {
            CustomerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
        }

        public async Task<IActionResult> Run()
        {
            Output = await usersRepository.CreateCharacter(CustomerGUID, UserSessionGUID, CharacterName, ClassName);

            return new OkObjectResult(Output);
        }
    }
}
