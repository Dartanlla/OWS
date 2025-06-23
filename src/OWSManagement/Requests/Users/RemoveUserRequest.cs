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
    public class RemoveUserRequest
    {
        private readonly Guid _customerGuid;
        private RemoveUserDTO _removeUserDTO { get; set; }
        private readonly IUsersRepository _usersRepository;        

        public RemoveUserRequest(Guid customerGuid, RemoveUserDTO removeUserDTO, IUsersRepository usersRepository)
        {
            _customerGuid = customerGuid;
            _removeUserDTO = removeUserDTO;
            _usersRepository = usersRepository;
        }

        public async Task<SuccessAndErrorMessage> Handle()
        {
            return await _usersRepository.RemoveUser(_customerGuid, _removeUserDTO.UserGUID, _removeUserDTO.Email);
        }
    }
}
