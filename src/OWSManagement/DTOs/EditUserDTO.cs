using System;

namespace OWSManagement.DTOs
{
    public class EditUserDTO
    {
        public Guid UserGUID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}
