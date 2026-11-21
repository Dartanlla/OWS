using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using Xunit;
using static OWSTests.Api.ApiTestHost;

namespace OWSTests.Api;

[Collection(ApiTestCollection.Name)]
public class PluginTravelTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectedCharacterWaitsForTheNewZoneAndJoinsWhenReady(bool lambda)
    {
        const string characterName = "Aster";
        const int mapInstanceId = 17;
        const int worldServerId = 3;
        const int port = 7777;
        const string usersPath = "/api/Users/";
        const string instancePath = "/api/Instance/";
        string selected = null;
        int instanceStatus = 1;
        int launches = 0;
        int assignments = 0;

        var instance = new ApiTestHost();
        instance.Add<IInstanceLauncherMessaging>((method, args) =>
        {
            Assert.Equal(nameof(IInstanceLauncherMessaging.PublishSpinUpServerMessage), method.Name);
            Assert.Equal(new object[] { Tenant, worldServerId, mapInstanceId, "UnrealHarbor", port }, args);
            launches++;
            return null;
        });
        instance.Add<IInstanceManagementRepository>((method, args) =>
        {
            Assert.Equal(nameof(IInstanceManagementRepository.SetZoneInstanceStatus), method.Name);
            Assert.Equal(new object[] { Tenant, mapInstanceId, 2 }, args);
            Assert.Equal(1, launches);
            instanceStatus = 2;
            return Task.FromResult(new SuccessAndErrorMessage { Success = true });
        });
        using var instanceServer = lambda ? null : instance.Start(ApiService.Instance);

        var publicApi = PublicApiTests.PlayerHost(
            session: () => PublicApiTests.PlayerSession(selected),
            userCall: (method, args) =>
            {
                Assert.Equal(nameof(IUsersRepository.UserSessionSetSelectedCharacter), method.Name);
                Assert.Equal(new object[] { Tenant, Session, 42 }, args);
                selected = characterName;
                return Task.FromResult(new SuccessAndErrorMessage { Success = true });
            },
            characterCall: (method, args) =>
            {
                switch (method.Name)
                {
                    case nameof(ICharactersRepository.GetCharByCharName):
                        Assert.Equal(new object[] { Tenant, characterName }, args);
                        return Task.FromResult(new GetCharByCharName
                        {
                            CustomerGuid = Tenant, UserGuid = Owner, CharacterId = 42, CharName = characterName, MapName = "Harbor"
                        });
                    case nameof(ICharactersRepository.JoinMapByCharacterId):
                        Assert.Equal(new object[] { Tenant, 42, Owner, "Harbor", 0 }, args);
                        Assert.Equal(characterName, selected);
                        return Task.FromResult(new JoinMapByCharName
                        {
                            WorldServerID = worldServerId, MapInstanceID = mapInstanceId, MapInstanceStatus = instanceStatus,
                            ServerIP = "192.0.2.44", Port = port, NeedToStartupMap = true, MapNameToStart = "UnrealHarbor"
                        });
                    case nameof(ICharactersRepository.CheckMapInstanceStatus):
                        Assert.Equal(new object[] { Tenant, mapInstanceId }, args);
                        return Task.FromResult(new MapInstances { MapInstanceId = mapInstanceId, Status = instanceStatus });
                    case nameof(ICharactersRepository.AddOwnedCharacterToMapInstance):
                        Assert.Equal(new object[] { Tenant, 42, Owner, mapInstanceId }, args);
                        Assert.Equal(2, instanceStatus);
                        assignments++;
                        return Task.FromResult(true);
                    default:
                        throw new InvalidOperationException(method.Name);
                }
            });

        async Task<ApiReply> SendInstance(string path, object body, string tenant = null) =>
            lambda
                ? await instance.SendLambdaAsync(ApiService.Instance, "POST", path, body, tenant)
                : await SendAsync(instanceServer, "POST", path, body, tenant);

        using var instanceClient = new InstanceClient(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(instancePath + "SpinUpServerInstance", request.RequestUri.AbsolutePath);
            var tenant = Assert.Single(request.Headers.GetValues("X-CustomerGUID"));
            var reply = await SendInstance(request.RequestUri.AbsolutePath, await request.Content.ReadAsStringAsync(), tenant);
            reply.Success();
            return new HttpResponseMessage((HttpStatusCode)reply.Status) { Content = new StringContent(reply.Body) };
        });
        publicApi.Add<IHttpClientFactory>(instanceClient);
        using var publicServer = lambda ? null : publicApi.Start(ApiService.Public);

        Task<ApiReply> SendPublic(string action, object body) => lambda
            ? publicApi.SendLambdaAsync(ApiService.Public, "POST", usersPath + action, body)
            : SendAsync(publicServer, "POST", usersPath + action, body);

        var session = (await SendPublic("SetSelectedCharacterAndGetUserSession", new
        {
            userSessionGUID = Session, selectedCharacterName = characterName
        })).Json();
        var travel = (await SendPublic("GetServerToConnectTo", new
        {
            userSessionGUID = session.Field("UserSessionGUID").GetGuid(),
            characterName = session.Field("CharName").GetString(),
            zoneName = "GETLASTZONENAME",
            playerGroupType = 0
        })).Json();
        Assert.Equal(mapInstanceId, travel.Field("MapInstanceID").GetInt32());
        Assert.Equal(port, travel.Field("Port").GetInt32());
        Assert.Equal(1, travel.Field("MapInstanceStatus").GetInt32());
        Assert.Equal(1, launches);
        Assert.Equal(0, assignments);

        var poll = new { userSessionGUID = Session, characterName, mapInstanceID = travel.Field("MapInstanceID").GetInt32() };
        Assert.Equal(1, (await SendPublic("GetServerToConnectToStatus", poll)).Json().Field("MapInstanceStatus").GetInt32());
        Assert.Equal(0, assignments);
        (await SendInstance(instancePath + "SetZoneInstanceStatus",
            new { request = new { zoneInstanceID = mapInstanceId, instanceStatus = 2 } })).Success();
        JsonElement ready = (await SendPublic("GetServerToConnectToStatus", poll)).Json();
        Assert.Equal(2, ready.Field("MapInstanceStatus").GetInt32());
        Assert.True(ready.Field("Success").GetBoolean());
        Assert.Equal(1, assignments);
        Assert.Equal(1, launches);
    }
}
