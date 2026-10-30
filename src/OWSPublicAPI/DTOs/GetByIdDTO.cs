namespace OWSPublicAPI.DTOs
{
    /// <summary>
    /// User session for the character lookup whose CharacterId is supplied in the route.
    /// </summary>
    public class GetByIdDTO
    {
        /// <summary>
        /// The user session that must own the requested character.
        /// </summary>
        public string UserSessionGUID { get; set; }
    }
}
