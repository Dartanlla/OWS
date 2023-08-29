using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Requests.Characters
{
    public class AddQuestListToDatabaseRequest
    {
        public IEnumerable<AddQuestListToDabase> questsToAdd { get; set; }

        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<SuccessAndErrorMessage> Handle()
        {
            await charactersRepository.AddQuestListToDatabase(customerGUID, questsToAdd);

            SuccessAndErrorMessage output = new SuccessAndErrorMessage();
            output.Success = true;
            output.ErrorMessage = "";
            return output;
        }
    }
}
