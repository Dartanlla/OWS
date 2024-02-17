using OWSData.Models.Composites;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSManagement.DTOs;

namespace OWSManagement.Services
{
    public interface IUsersService
    {
        Task<IEnumerable<User>> GetUsersAsync();
        Task<SuccessAndErrorMessage> AddUserAsync(AddUserDTO addUserDTO);
        Task<SuccessAndErrorMessage> EditUserAsync(EditUserDTO editUserDTO);
    }
    public class UsersService : IUsersService
    {
        private readonly Guid _customerGuid;
        private readonly IUsersRepository _usersRepository;

        public UsersService(IUsersRepository usersRepository, IConfiguration config)
        {
            _customerGuid = new Guid(config["OWSManagementOptions:OWSAPIKey"]);
            _usersRepository = usersRepository;
        }
        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await _usersRepository.GetUsers(_customerGuid);
        }

        public async Task<SuccessAndErrorMessage> AddUserAsync(AddUserDTO addUserDTO)
        {
            return await _usersRepository.RegisterUser(_customerGuid, addUserDTO.Email, addUserDTO.Password, addUserDTO.FirstName, addUserDTO.LastName);
        }

        public async Task<SuccessAndErrorMessage> EditUserAsync(EditUserDTO editUserDTO)
        {
            return await _usersRepository.UpdateUser(_customerGuid, editUserDTO.UserGUID, editUserDTO.FirstName, editUserDTO.LastName, editUserDTO.Email);
        }
    }
}
