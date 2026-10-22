using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using System;
using System.Threading.Tasks;

namespace OWSCharacterPersistence.Requests.Characters
{
    public class UpdateCharacterClassRequest : IOwnedCharacterWrite
    {
        public string CharacterName { get; set; }
        public Guid? OwnerToken { get; set; }
        string IOwnedCharacterWrite.OwnedCharacterName => CharacterName;

        public string ClassName { get; set; }

        private Guid customerGUID;
        private ICharactersRepository charactersRepository;

        public void SetData(ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            this.charactersRepository = charactersRepository;
            customerGUID = customerGuid.CustomerGUID;
        }

        public async Task<SuccessAndErrorMessage> Handle()
        {
            SuccessAndErrorMessage successAndErrorMessage = new SuccessAndErrorMessage();
            successAndErrorMessage.Success = true;

            try
            {
                await charactersRepository.UpdateCharacterClass(customerGUID, CharacterName, ClassName);
            }
            catch (Exception ex)
            {
                successAndErrorMessage.ErrorMessage = ex.Message;
                successAndErrorMessage.Success = false;
            }

            return successAndErrorMessage;
        }
    }
}
