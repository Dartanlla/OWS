using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OWSManagement.Requests.PlayerCharacters
{
    public class GetCharactersRequest
    {
        private readonly Guid _customerGuid;
        private readonly ICharactersRepository _charactersRepository;

        public GetCharactersRequest(Guid customerGuid, ICharactersRepository charactersRepository)
        {
            _customerGuid = customerGuid;
            _charactersRepository = charactersRepository;
        }
        public async Task<IEnumerable<Characters>> Handle()
        {
            return await _charactersRepository.GetCharacters(_customerGuid); ;
        }
    }
}
