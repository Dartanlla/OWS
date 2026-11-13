using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.Requests.Users;
using OWSShared.Implementations;
using Xunit;

namespace OWSTests.Characters
{
    public class CharacterSelectionTests
    {
        [Theory]
        [InlineData(false, false, false, 404, 0, 1)]
        [InlineData(true, false, false, 404, 0, 1)]
        [InlineData(false, true, false, 409, 1, 1)]
        [InlineData(true, true, false, 409, 1, 1)]
        [InlineData(false, true, true, 200, 1, 1)]
        [InlineData(true, true, true, 200, 1, 2)]
        public async Task BothRoutesRequireOwnershipAndSuccessfulUpdate(
            bool combined, bool ownsCharacter, bool updateSucceeds, int status, int expectedUpdates, int expectedReads)
        {
            var tenant = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            var owner = Guid.NewGuid();
            var session = new GetUserSession { CustomerGuid = tenant, UserSessionGUID = sessionId, UserGuid = owner };
            int updates = 0;
            int reads = 0;
            var users = RepositoryProxy.Create<IUsersRepository>((method, args) =>
            {
                Assert.Equal(tenant, args[0]);
                Assert.Equal(sessionId, args[1]);
                if (method.Name == nameof(IUsersRepository.GetUserSession))
                {
                    reads++;
                    return Task.FromResult(session);
                }

                Assert.Equal(nameof(IUsersRepository.UserSessionSetSelectedCharacter), method.Name);
                Assert.Equal(42, args[2]);
                updates++;
                return Task.FromResult(new SuccessAndErrorMessage { Success = updateSucceeds });
            });
            var characters = RepositoryProxy.Create<ICharactersRepository>((method, args) =>
            {
                Assert.Equal(nameof(ICharactersRepository.GetCharByCharName), method.Name);
                Assert.Equal(tenant, args[0]);
                Assert.Equal("Player", args[1]);
                return Task.FromResult(new GetCharByCharName
                {
                    CustomerGuid = tenant, CharacterId = 42, UserGuid = ownsCharacter ? owner : Guid.NewGuid()
                });
            });
            var customer = new HeaderCustomerGUID { CustomerGUID = tenant };

            IActionResult result;
            if (combined)
            {
                var request = new SetSelectedCharacterAndGetUserSessionRequest
                {
                    UserSessionGUID = sessionId, SelectedCharacterName = "Player"
                };
                request.SetData(users, characters, customer);
                result = await request.Handle();
            }
            else
            {
                var request = new UserSessionSetSelectedCharacterRequest
                {
                    UserSessionGUID = sessionId, SelectedCharacterName = "Player"
                };
                request.SetData(users, characters, customer);
                result = await request.Handle();
            }

            Assert.Equal(status, result is ObjectResult response ? response.StatusCode :
                Assert.IsAssignableFrom<StatusCodeResult>(result).StatusCode);
            Assert.Equal(expectedUpdates, updates);
            Assert.Equal(expectedReads, reads);
        }
    }
}
