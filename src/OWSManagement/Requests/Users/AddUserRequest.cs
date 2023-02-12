using OWSData.Models.Composites;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSManagement.DTOs;
using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OWSManagement.Requests.Users
{
    public class AddUserRequest
    {
        private readonly Guid _customerGuid;
        private AddUserDTO _addUserDTO { get; set; }
        private readonly IUsersRepository _usersRepository;        

        public AddUserRequest(Guid customerGuid, AddUserDTO addUserDTO, IUsersRepository usersRepository)
        {
            _customerGuid = customerGuid;
            _addUserDTO = addUserDTO;
            _usersRepository = usersRepository;
        }

        public async Task<SuccessAndErrorMessage> Handle()
        {
            return await _usersRepository.RegisterUser(_customerGuid, _addUserDTO.Email, _addUserDTO.Password, _addUserDTO.FirstName, _addUserDTO.LastName);
        }
    }
}
