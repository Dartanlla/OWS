using System;

namespace OWSManagement.DTOs
{
    public class RemoveUserDTO
    {
        public Guid UserGUID { get; set; }
        public string Email { get; set; }
    }
}
