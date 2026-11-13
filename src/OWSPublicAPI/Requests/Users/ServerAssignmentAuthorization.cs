using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;

namespace OWSPublicAPI.Requests.Users
{
    internal static class ServerAssignmentAuthorization
    {
        internal static async Task<(GetCharByCharName Character, IActionResult Error)> CheckAsync(
            IUsersRepository users, ICharactersRepository characters, Guid customerGuid,
            Guid sessionGuid, string characterName)
        {
            if (customerGuid == Guid.Empty || sessionGuid == Guid.Empty)
                return (null, new UnauthorizedResult());

            if (string.IsNullOrWhiteSpace(characterName))
                return (null, new BadRequestResult());

            var session = await users.GetUserSession(customerGuid, sessionGuid);
            if (session == null || session.CustomerGuid != customerGuid ||
                session.UserSessionGUID != sessionGuid ||
                session.UserGuid is not Guid owner || owner == Guid.Empty)
                return (null, new UnauthorizedResult());

            var character = await characters.GetCharByCharName(customerGuid, characterName);
            if (character == null || character.CustomerGuid != customerGuid ||
                character.UserGuid != owner)
                return (null, new NotFoundResult());

            return (character, null);
        }
    }
}
