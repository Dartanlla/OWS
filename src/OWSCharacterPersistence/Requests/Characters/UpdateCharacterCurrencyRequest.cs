using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Requests.Characters
{
    public class UpdateCharacterCurrencyRequest : IOwnedCharacterWrite
    {
        public string CharacterName { get; set; }
        public Guid? OwnerToken { get; set; }
        string IOwnedCharacterWrite.OwnedCharacterName => CharacterName;

        public int Gold { get; set; }

        // Opt-in: the Characters.EconomyRevision this wallet was computed from. Sent, the write is
        // refused with ReasonCode "stale_revision" when it no longer matches rather than overwriting a
        // shop transaction that committed in between. Omitted (null), behaviour is legacy last-write-wins.
        public long? ExpectedRevision { get; set; }

        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<UpdateCharacterCurrencyResponse> Handle()
        {
            UpdateCharacterCurrencyResponse response = new UpdateCharacterCurrencyResponse();
            response.Success = true;

            try
            {
                response = await charactersRepository.UpdateCharacterCurrency(customerGUID, CharacterName, Gold, ExpectedRevision);
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                response.Success = false;
            }

            return response;
        }
    }
}
