using System;

namespace OWSCharacterPersistence.Requests.Characters
{
    /// <summary>
    /// A zone save for one character. Once the character has an owner (CharacterOwners), CharactersController refuses
    /// it with stale_owner unless OwnerToken is the current owner's token.
    /// </summary>
    public interface IOwnedCharacterWrite
    {
        string OwnedCharacterName { get; }
        Guid? OwnerToken { get; }
    }
}
