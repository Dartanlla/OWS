namespace OWSPublicAPI.DTOs
{
    /// <summary>
    /// GetByNameDTO data transfer object
    /// </summary>
    /// <remarks>
    /// GetByNameDTO is data transfer object for GetByNameRequest
    /// </remarks>
    public class GetDefaultCustomrDataDTO
    {
        /// <summary>
        /// UserSessionGUID Request Parameter
        /// </summary>
        /// <remarks>
        /// Contains the User Session GUID from the request
        /// </remarks>
        public string UserSessionGUID { get; set; }

        /// <summary>
        /// CharacterName Request Paramater
        /// </summary>
        /// <remarks>
        /// Contains the Character Name from the request
        /// </remarks>
        public string DefaultSetName { get; set; }
    }
}
