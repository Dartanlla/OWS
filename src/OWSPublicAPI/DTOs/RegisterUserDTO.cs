namespace OWSPublicAPI.DTOs
{
    /// <summary>
    /// RegisterUser Data Transfer Object
    /// </summary>
    /// <remarks>
    /// This object is for collecting POST request parameters
    /// </remarks>
    public class RegisterUserDTO
    {
        /// <summary>
        /// Email
        /// </summary>
        /// <remarks>
        /// Email for the user.  This value is not meant to be displayed in game.
        /// </remarks>
        public string Email { get; set; }
        /// <summary>
        /// Password
        /// </summary>
        /// <remarks>
        /// Password for the user.  Passwords are one way encrypted with SHA 256 and a 25 character Salt when using the MSSQL implementation of UsersRepository.
        /// </remarks>
        public string Password { get; set; }
        /// <summary>
        /// First Name
        /// </summary>
        /// <remarks>
        /// First Name for the user.  This value is not meant to be displayed in game.
        /// </remarks>
        public string FirstName { get; set; }
        /// <summary>
        /// Last Name
        /// </summary>
        /// <remarks>
        /// Last Name for the user.  This value is not meant to be displayed in game.
        /// </remarks>
        public string LastName { get; set; }
    }
}
