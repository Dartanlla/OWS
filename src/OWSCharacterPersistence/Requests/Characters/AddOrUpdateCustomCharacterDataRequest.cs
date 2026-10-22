using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Requests.Characters
{
    public class AddOrUpdateCustomDataRequest : IOwnedCharacterWrite
    {
        public AddOrUpdateCustomCharacterData addOrUpdateCustomCharacterData { get; set; }
        public Guid? OwnerToken { get; set; }
        string IOwnedCharacterWrite.OwnedCharacterName => addOrUpdateCustomCharacterData?.CharacterName;

        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task Handle()
        {
            await charactersRepository.AddOrUpdateCustomCharacterData(customerGUID, addOrUpdateCustomCharacterData);

            return;
        }
    }
}