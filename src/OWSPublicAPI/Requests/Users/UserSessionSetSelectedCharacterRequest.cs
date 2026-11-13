using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Requests.Users
{
    public class UserSessionSetSelectedCharacterRequest : IRequestHandler<UserSessionSetSelectedCharacterRequest, IActionResult>, IRequest
    {
        public Guid UserSessionGUID { get; set; }
        public string SelectedCharacterName { get; set; }

        private Guid customerGUID;
        private IUsersRepository usersRepository;
        private ICharactersRepository charactersRepository;

        public void SetData(IUsersRepository usersRepository, ICharactersRepository charactersRepository, IHeaderCustomerGUID customerGuid)
        {
            customerGUID = customerGuid.CustomerGUID;
            this.usersRepository = usersRepository;
            this.charactersRepository = charactersRepository;
        }

        public async Task<IActionResult> Handle()
        {
            var (update, error) = await SelectAsync(
                usersRepository, charactersRepository, customerGUID, UserSessionGUID, SelectedCharacterName);
            return error ?? new OkObjectResult(update);
        }

        internal static async Task<(SuccessAndErrorMessage Update, IActionResult Error)> SelectAsync(
            IUsersRepository usersRepository, ICharactersRepository charactersRepository,
            Guid customerGuid, Guid sessionGuid, string characterName)
        {
            var (character, error) = await PlayerAuthorization.CheckAsync(
                usersRepository, charactersRepository, customerGuid, sessionGuid, characterName);
            if (error != null)
                return (null, error);

            var update = await usersRepository.UserSessionSetSelectedCharacter(customerGuid, sessionGuid, character.CharacterId);
            return update.Success ? (update, null) : (null, new ConflictObjectResult(update));
        }
    }
}
