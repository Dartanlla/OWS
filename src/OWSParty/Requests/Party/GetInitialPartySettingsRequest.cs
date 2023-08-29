using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using PartyServiceApp.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWSParty.Requests.Party
{
    public class GetInitialPartySettingsRequest
    {
        private Guid _customerGUID;
        private ICharactersRepository _charactersRepository;

        private string _charName;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, string charName)
        {
            _charactersRepository = charactersRepository;
            _customerGUID = customerGuid.CustomerGUID;
            _charName = charName;
        }

        public async Task<PartyToSend> Handle()
        {
            PartyToSend partyInformation = await _charactersRepository.GetInitialPartySettings(_customerGUID, _charName);

            return partyInformation;
        }
    }
}
