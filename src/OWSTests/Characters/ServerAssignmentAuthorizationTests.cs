using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.Requests.Users;
using OWSShared.Implementations;
using Xunit;

namespace OWSTests.Characters
{
    public class ServerAssignmentAuthorizationTests
    {
        [Theory]
        [InlineData(false, "missing session", 401)]
        [InlineData(true, "missing session", 401)]
        [InlineData(false, "revoked session", 401)]
        [InlineData(true, "revoked session", 401)]
        [InlineData(false, "wrong tenant", 401)]
        [InlineData(true, "wrong tenant", 401)]
        [InlineData(false, "wrong owner", 404)]
        [InlineData(true, "wrong owner", 404)]
        [InlineData(false, "owner", 200)]
        [InlineData(true, "owner", 200)]
        public async Task OnlyAnAuthenticatedOwnerCanAssignCharacter(bool poll, string scenario, int status)
        {
            var tenant = Guid.NewGuid();
            var session = Guid.NewGuid();
            var owner = Guid.NewGuid();
            var customer = new HeaderCustomerGUID { CustomerGUID = tenant };
            var users = RepositoryProxy.Create<IUsersRepository>((method, args) =>
            {
                Assert.Equal(nameof(IUsersRepository.GetUserSession), method.Name);
                Assert.NotEqual("missing session", scenario);
                Assert.Equal(tenant, args[0]);
                Assert.Equal(session, args[1]);
                return Task.FromResult(scenario == "revoked session" ? null : new GetUserSession
                {
                    CustomerGuid = scenario == "wrong tenant" ? Guid.NewGuid() : tenant,
                    UserSessionGUID = session,
                    UserGuid = owner
                });
            });
            var assignments = 0;
            var characters = RepositoryProxy.Create<ICharactersRepository>((method, args) =>
            {
                Assert.Equal(tenant, args[0]);
                if (method.Name == nameof(ICharactersRepository.GetCharByCharName))
                {
                    Assert.True(status == 200 || status == 404);
                    return Task.FromResult(new GetCharByCharName
                    {
                        CustomerGuid = tenant,
                        CharName = "Character",
                        UserGuid = scenario == "wrong owner" ? Guid.NewGuid() : owner
                    });
                }

                Assert.Equal(200, status);
                switch (method.Name)
                {
                    case nameof(ICharactersRepository.JoinMapByCharName):
                        return Task.FromResult(new JoinMapByCharName
                        {
                            WorldServerID = 1, MapInstanceID = 42, MapInstanceStatus = 2
                        });
                    case nameof(ICharactersRepository.CheckMapInstanceStatus):
                        return Task.FromResult(new MapInstances { Status = 2 });
                    case nameof(ICharactersRepository.AddCharacterToMapInstanceByCharName):
                        Assert.Equal("Character", args[1]);
                        Assert.Equal(42, args[2]);
                        assignments++;
                        return Task.CompletedTask;
                    default:
                        throw new InvalidOperationException(method.Name);
                }
            });

            var sessionGuid = scenario == "missing session" ? Guid.Empty : session;
            IActionResult result;
            if (poll)
            {
                var request = new GetServerToConnectToStatusRequest
                {
                    UserSessionGUID = sessionGuid, CharacterName = "Character", MapInstanceID = 42
                };
                request.SetData(users, characters, customer);
                result = await request.Handle();
            }
            else
            {
                var request = new GetServerToConnectToRequest
                {
                    UserSessionGUID = sessionGuid, CharacterName = "Character", ZoneName = "Zone"
                };
                request.SetData(users, characters, customer, null);
                result = await request.Handle();
            }

            Assert.Equal(status, result is OkObjectResult ok ? ok.StatusCode : Assert.IsAssignableFrom<StatusCodeResult>(result).StatusCode);
            Assert.Equal(status == 200 ? 1 : 0, assignments);
        }
    }
}
