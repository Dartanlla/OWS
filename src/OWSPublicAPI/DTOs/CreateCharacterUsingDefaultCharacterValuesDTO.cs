using System;

namespace OWSPublicAPI.DTOs
{
    /// <summary>
    /// CreateCharacterUsingDefaultCharacterValues Data Transfer Object
    /// </summary>
    /// <remarks>
    /// The object is used to collect input from the CreateCharacterUsingDefaultCharacterValues Request
    /// </remarks>
    public class CreateCharacterUsingDefaultCharacterValuesDTO
    {
        /// <summary>
        /// UserSessionGUID Request Paramater.
        /// </summary>
        /// <remarks>
        /// Contains the User Session GUID from the request.  This identifies the User we are modifying.
        /// </remarks>
        public Guid UserSessionGUID { get; set; }
        /// <summary>
        /// CharacterName Request Paramater.
        /// </summary>
        /// <remarks>
        /// Contains the Character Name from the request.  This is the new Character Name to create.
        /// </remarks>
        public string CharacterName { get; set; }
        /// <summary>
        /// Default Set Name Paramater.
        /// </summary>
        /// <remarks>
        /// Contains the Default Set Name from the request.  This sets the default values for the new Character.
        /// </remarks>
        public string DefaultSetName { get; set; }
    }
}
