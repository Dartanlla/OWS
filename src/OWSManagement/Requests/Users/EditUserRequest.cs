using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSManagement.DTOs;
using System.Threading.Tasks;
using System;

namespace OWSManagement.Requests.Users
{
    public class EditUserRequest
    {
        private readonly Guid _customerGuid;
        private EditUserDTO _editUserDTO { get; set; }
        private readonly IUsersRepository _usersRepository;

        public EditUserRequest(Guid customerGuid, EditUserDTO editUserDTO, IUsersRepository usersRepository)
        {
            _customerGuid = customerGuid;
            _editUserDTO = editUserDTO;
            _usersRepository = usersRepository;
        }

        public async Task<SuccessAndErrorMessage> Handle()
        {
            return await _usersRepository.UpdateUser(_customerGuid, _editUserDTO.UserGUID, _editUserDTO.FirstName, _editUserDTO.LastName, _editUserDTO.Email);
        }
    }
}
