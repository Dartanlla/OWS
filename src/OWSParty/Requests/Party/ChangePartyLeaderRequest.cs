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
    public class ChangePartyLeaderRequest
    {
        private PartyToSend _partyRequest;
        private Guid _customerGUID;
        private ICharactersRepository _charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid, PartyToSend partyRequest)
        {
            _charactersRepository = charactersRepository;
            _customerGUID = customerGuid.CustomerGUID;
            _partyRequest = partyRequest;
        }

        public async Task<PartyToSend> Handle()
        {
            PartyToSend partyInformation = await _charactersRepository.ChangePartyLeader(_customerGUID, _partyRequest);

            return partyInformation;
        }
    }
}
