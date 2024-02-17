using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;

namespace OWSManagement.Services { 
    public interface ICharactersService
    {
        Task<IEnumerable<GetAllCharacters>> GetCharactersAsync(String email);
    }

    public class CharactersService : ICharactersService
    {
        private readonly Guid _customerGUID;
        private readonly ICharactersRepository _charactersRepository;
        private readonly IUsersRepository _usersRepository;

        public CharactersService(ICharactersRepository charactersRepository, IUsersRepository usersRepository, IConfiguration config)
        {
            _customerGUID = new Guid(config["OWSManagementOptions:OWSAPIKey"]);
            _charactersRepository = charactersRepository;
            _usersRepository = usersRepository;
        }
        public async Task<IEnumerable<GetAllCharacters>> GetCharactersAsync(String email)
        {
            var user = await _usersRepository.GetUserFromEmail(_customerGUID, email);

            if (user != null) { 
            var allCharacters = await _usersRepository.GetAllCharacters(_customerGUID, user.UserSessionGUID);
            return allCharacters;
            }
            else
            {
                return [];
            }

        }
    }
}