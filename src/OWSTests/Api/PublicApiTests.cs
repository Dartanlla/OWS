using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OWSData.Models.Composites;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using Xunit;
using static OWSTests.Api.ApiTestHost;

namespace OWSTests.Api;

[Collection(ApiTestCollection.Name)]
public class PublicApiTests
{
    private const string Users = "/api/Users/";
    private const string Characters = "/api/Characters/";
    private const string Player = "Aster";
    private const string Email = "aster@example.test";
    private const string Password = "example-password";
    private static readonly Guid OtherOwner = Guid.Parse("49829479-5e3d-4503-bb97-867ca2604579");
    private static readonly SuccessAndErrorMessage Success = new() { Success = true, ErrorMessage = "" };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PasswordLoginReportsAuthenticationAndSession(bool authenticated)
    {
        var host = PlayerHost(userCall: (method, args) =>
        {
            Assert.Equal(nameof(IUsersRepository.LoginAndCreateSession), method.Name);
            Assert.Equal(new object[] { Tenant, Email, Password, false }, args);
            return Task.FromResult(new PlayerLoginAndCreateSession
            {
                Authenticated = authenticated, UserSessionGuid = authenticated ? Session : Guid.Empty
            });
        });

        await host.VerifyAsync(ApiService.Public, "POST", Users + "LoginAndCreateSession", new { Email, Password }, reply =>
        {
            CheckLogin(reply, authenticated);
            if (!authenticated)
                Assert.Contains("invalid", reply.Json().Field("ErrorMessage").GetString(), StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task DisabledExternalLoginReturnsAnUnauthenticatedSession()
    {
        var host = new ApiTestHost();
        await host.VerifyAsync(ApiService.Public, "POST", Users + "ExternalLoginAndCreateSession",
            new { Email, Password }, reply => CheckLogin(reply, false));
    }

    [Theory]
    [InlineData("registered")]
    [InlineData("duplicate")]
    [InlineData("write-failed")]
    public async Task RegistrationCreatesAndLogsInANewAccountOnlyAfterASuccessfulWrite(string outcome)
    {
        var calls = new List<string>();
        var host = PlayerHost(userCall: (method, args) =>
        {
            calls.Add(method.Name);
            switch (method.Name)
            {
                case nameof(IUsersRepository.GetUserFromEmail):
                    Assert.Equal(new object[] { Tenant, Email }, args);
                    return Task.FromResult(outcome == "duplicate" ? PlayerSession() : null);
                case nameof(IUsersRepository.RegisterUser):
                    Assert.Equal(new object[] { Tenant, Email, Password, "Aster", "Example" }, args);
                    return Task.FromResult(new SuccessAndErrorMessage { Success = outcome != "write-failed", ErrorMessage = "Storage error" });
                case nameof(IUsersRepository.LoginAndCreateSession):
                    Assert.Equal(new object[] { Tenant, Email, Password, false }, args);
                    return Task.FromResult(new PlayerLoginAndCreateSession { Authenticated = true, UserSessionGuid = Session });
                default:
                    throw new InvalidOperationException(method.Name);
            }
        });
        await host.VerifyAsync(ApiService.Public, "POST", Users + "RegisterUser",
            new { Email, Password, FirstName = "Aster", LastName = "Example" }, reply =>
            {
                var json = reply.Json();
                Assert.Equal(outcome == "registered", json.Field("Authenticated").GetBoolean());
                if (outcome == "registered")
                    Assert.Equal(Session, json.Field("UserSessionGuid").GetGuid());
                else
                    Assert.False(string.IsNullOrWhiteSpace(json.Field("ErrorMessage").GetString()));

                string[] expected = outcome switch
                {
                    "registered" => ["GetUserFromEmail", "RegisterUser", "LoginAndCreateSession"],
                    "write-failed" => ["GetUserFromEmail", "RegisterUser"],
                    _ => ["GetUserFromEmail"]
                };
                Assert.Equal(expected, calls);
                calls.Clear();
            });
    }

    [Theory]
    [InlineData("CreateCharacter", "Aster", false, true)]
    [InlineData("CreateCharacter", "A!", false, false)]
    [InlineData("CreateCharacterUsingDefaultCharacterValues", "Aster", false, true)]
    [InlineData("CreateCharacterUsingDefaultCharacterValues", "Aster", true, false)]
    [InlineData("CreateCharacterUsingDefaultCharacterValues", "Invalid Name", false, false)]
    public async Task CharacterCreationValidatesNamesAndUsesTheRequestedDefaults(string action, string name, bool exists, bool created)
    {
        int writes = 0;
        var host = PlayerHost(
            userCall: (method, args) =>
            {
                Assert.Equal(action, method.Name);
                writes++;
                if (action == "CreateCharacter")
                {
                    Assert.Equal(new object[] { Tenant, Session, name, "Ranger" }, args);
                    return Task.FromResult(new CreateCharacter { Success = true, CharacterName = name, ClassName = "Ranger", StartingMapName = "Harbor" });
                }

                Assert.Equal(new object[] { Tenant, Owner, name, "NewPlayer" }, args);
                return Task.FromResult(Success);
            },
            characterCall: (method, args) =>
            {
                Assert.Equal(nameof(ICharactersRepository.GetCharByCharName), method.Name);
                Assert.Equal(new object[] { Tenant, name }, args);
                return Task.FromResult(exists ? PlayerCharacter() : null);
            });

        await host.VerifyAsync(ApiService.Public, "POST", Users + action,
            new { UserSessionGUID = Session, CharacterName = name, ClassName = "Ranger", DefaultSetName = "NewPlayer" }, reply =>
            {
                var json = reply.Json();
                Assert.Equal(created, json.Field("Success").GetBoolean());
                if (created && action == "CreateCharacter")
                    Assert.Equal("Harbor", json.Field("StartingMapName").GetString());
                if (!created)
                    Assert.NotEmpty(json.Field("ErrorMessage").GetString());
                Assert.Equal(created ? 1 : 0, writes);
                writes = 0;
            });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CharacterListUsesPluginFieldNamesAndPreservesAnEmptyRoster(bool empty)
    {
        var host = PlayerHost(userCall: (method, args) =>
        {
            Assert.Equal(nameof(IUsersRepository.GetAllCharacters), method.Name);
            Assert.Equal(new object[] { Tenant, Session }, args);
            return Task.FromResult<IEnumerable<GetAllCharacters>>(
                empty ? [] : [new() { CharacterId = 42, CharName = Player, MapName = "Harbor", ClassName = "Ranger" }]);
        });
        await host.VerifyAsync(ApiService.Public, "POST", Users + "GetAllCharacters", new { UserSessionGUID = Session }, reply =>
        {
            var rows = reply.Json();
            Assert.Equal(empty ? 0 : 1, rows.GetArrayLength());
            if (!empty)
            {
                Assert.Equal(Player, rows[0].GetProperty("characterName").GetString());
                Assert.Equal("Harbor", rows[0].GetProperty("zoneName").GetString());
                Assert.Equal("Ranger", rows[0].Field("ClassName").GetString());
            }
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task PlayerGroupsApplyThePluginsTypeFilterAndCurrentlyReturnABareArray(int groupType)
    {
        // The plugin callback expects an object with a rows array. The API currently emits the array directly.
        var host = PlayerHost(userCall: (method, args) =>
        {
            Assert.Equal(nameof(IUsersRepository.GetPlayerGroupsCharacterIsIn), method.Name);
            Assert.Equal(new object[] { Tenant, Session, 42, groupType }, args);
            return Task.FromResult<IEnumerable<GetPlayerGroupsCharacterIsIn>>(
                [new() { PlayerGroupID = 71, PlayerGroupName = "Explorers", PlayerGroupTypeID = 4 }]);
        });
        await host.VerifyAsync(ApiService.Public, "POST", Users + "GetPlayerGroupsCharacterIsIn",
            new { UserSessionGUID = Session, CharacterName = Player, PlayerGroupTypeID = groupType }, reply =>
            {
                var group = Assert.Single(reply.Json().EnumerateArray());
                Assert.Equal(71, group.Field("PlayerGroupID").GetInt32());
                Assert.Equal("Explorers", group.Field("PlayerGroupName").GetString());
                Assert.Equal(4, group.Field("PlayerGroupTypeID").GetInt32());
            });
    }

    [Fact]
    public async Task CharacterSelectionReturnsTheUpdatedSessionAndLogoutInvalidatesIt()
    {
        string selected = null;
        bool active = true;
        var host = PlayerHost(
            session: () => active ? PlayerSession(selected) : null,
            userCall: (method, args) =>
            {
                switch (method.Name)
                {
                    case nameof(IUsersRepository.UserSessionSetSelectedCharacter):
                        Assert.Equal(new object[] { Tenant, Session, 42 }, args);
                        selected = Player;
                        break;
                    case nameof(IUsersRepository.Logout):
                        Assert.Equal(new object[] { Tenant, Session }, args);
                        active = false;
                        break;
                    default:
                        throw new InvalidOperationException(method.Name);
                }
                return Task.FromResult(Success);
            });
        var selection = new { UserSessionGUID = Session, SelectedCharacterName = Player };
        await host.VerifyAsync(ApiService.Public, "POST", Users + "UserSessionSetSelectedCharacter", selection,
            reply => { reply.Success(); Assert.Equal(Player, selected); selected = null; });
        await host.VerifyAsync(ApiService.Public, "POST", Users + "SetSelectedCharacterAndGetUserSession", selection, VerifySelectedSession);
        var sessionQuery = Users + "GetUserSession?UserSessionGUID=" + Session;
        await host.VerifyAsync(ApiService.Public, "GET", sessionQuery, null, VerifySelectedSession);
        await host.VerifyAsync(ApiService.Public, "POST", Users + "Logout", new { UserSessionGUID = Session }, reply =>
        {
            reply.Success();
            Assert.False(active);
            active = true;
        });

        active = false;
        await host.VerifyAsync(ApiService.Public, "GET", sessionQuery, null, reply => reply.Is(401));
        await host.VerifyAsync(ApiService.Public, "POST", Users + "Logout", new { UserSessionGUID = Session }, reply => reply.Is(401));
    }

    [Theory]
    [InlineData("RemoveCharacter", 200)]
    [InlineData("RemoveCharacter", 404)]
    [InlineData("GetPlayerGroupsCharacterIsIn", 404)]
    [InlineData("SetSelectedCharacterAndGetUserSession", 404)]
    public async Task CharacterOperationsRequireAnOwnedCharacter(string action, int expected)
    {
        int writes = 0;
        var host = PlayerHost(
            userCall: (method, args) =>
            {
                Assert.Equal(nameof(IUsersRepository.RemoveCharacter), method.Name);
                Assert.Equal(new object[] { Tenant, Session, 42 }, args);
                writes++;
                return Task.FromResult(Success);
            },
            characterCall: (method, args) => Task.FromResult(PlayerCharacter(expected == 404 ? OtherOwner : Owner)));
        await host.VerifyAsync(ApiService.Public, "POST", Users + action,
            new { UserSessionGUID = Session, CharacterName = Player, SelectedCharacterName = Player }, reply =>
            {
                reply.Is(expected);
                if (expected == 200) reply.Success();
                Assert.Equal(expected == 200 ? 1 : 0, writes);
                writes = 0;
            });
    }

    [Theory]
    [InlineData("ByName")]
    [InlineData("42")]
    [InlineData("GetDefaultCustomData")]
    public async Task PublicCharacterDataIncludesOnlyClientVisibleFields(string action)
    {
        var host = PlayerHost(characterCall: (method, args) =>
        {
            switch (method.Name)
            {
                case nameof(ICharactersRepository.GetCharByCharName):
                    Assert.Equal(new object[] { Tenant, Player }, args);
                    return Task.FromResult(PlayerCharacter());
                case nameof(ICharactersRepository.GetCharByCharId):
                    Assert.Equal(new object[] { Tenant, 42 }, args);
                    return Task.FromResult(PlayerCharacter());
                case nameof(ICharactersRepository.GetOwnedCharacterCustomData):
                    Assert.Equal(new object[] { Tenant, 42, Owner }, args);
                    return Task.FromResult<IEnumerable<CustomCharacterData>>([
                        new() { CustomFieldName = "CosmeticCharacterData", FieldValue = "blue" },
                        new() { CustomFieldName = "Private", FieldValue = "hidden" }]);
                case nameof(ICharactersRepository.GetDefaultCustomCharacterData):
                    Assert.Equal(new object[] { Tenant, "NewPlayer" }, args);
                    return Task.FromResult<IEnumerable<DefaultCustomData>>([
                        new() { CustomFieldName = "BaseCharacterStats", FieldValue = """{"health":100}""" },
                        new() { CustomFieldName = "Private", FieldValue = "hidden" }]);
                default:
                    throw new InvalidOperationException(method.Name);
            }
        });

        await host.VerifyAsync(ApiService.Public, "POST", Characters + action,
            new { UserSessionGUID = Session, CharacterName = Player, DefaultSetName = "NewPlayer" }, reply =>
            {
                var json = reply.Json();
                if (action == "GetDefaultCustomData")
                {
                    var row = Assert.Single(json.Field("Rows").EnumerateArray());
                    Assert.Equal("BaseCharacterStats", row.Field("CustomFieldName").GetString());
                    Assert.Equal("""{"health":100}""", row.Field("FieldValue").GetString());
                    return;
                }

                Assert.Equal(42, json.Field("CharacterData").Field("CharacterId").GetInt32());
                Assert.Collection(json.Field("CustomCharacterDataRows").EnumerateArray(),
                    row =>
                    {
                        Assert.Equal("CosmeticCharacterData", row.Field("CustomFieldName").GetString());
                        Assert.Equal("blue", row.Field("FieldValue").GetString());
                    },
                    row => Assert.Equal("ReadOnlyCharacterData", row.Field("CustomFieldName").GetString()));
            });
    }

    [Theory]
    [InlineData("GETLASTZONENAME", "Harbor", 0, false, 2, 200)]
    [InlineData("", "Harbor", 0, false, 2, 200)]
    [InlineData("Dungeon", "Dungeon", 4, false, 1, 200)]
    [InlineData("Dungeon", "Dungeon", 4, true, 1, 200)]
    [InlineData("Dungeon", "Dungeon", 4, false, 2, 404)]
    public async Task ZoneTravelHandlesExistingAndStartingWorlds(string requestedZone, string zone, int group,
        bool spinUp, int status, int expected)
    {
        int assignments = 0;
        int launches = 0;
        var host = PlayerHost(characterCall: (method, args) =>
        {
            switch (method.Name)
            {
                case nameof(ICharactersRepository.GetCharByCharName):
                    Assert.Equal(new object[] { Tenant, Player }, args);
                    return Task.FromResult(PlayerCharacter());
                case nameof(ICharactersRepository.JoinMapByCharacterId):
                    Assert.Equal(new object[] { Tenant, 42, Owner, zone, group }, args);
                    return Task.FromResult(new JoinMapByCharName
                    {
                        WorldServerID = 3, MapInstanceID = 17, MapInstanceStatus = status,
                        NeedToStartupMap = spinUp, MapNameToStart = "UnrealDungeon", Port = 7777,
                        ServerIP = "192.0.2.44"
                    });
                case nameof(ICharactersRepository.AddOwnedCharacterToMapInstance):
                    Assert.Equal(new object[] { Tenant, 42, Owner, 17 }, args);
                    assignments++;
                    return Task.FromResult(expected == 200);
                default:
                    throw new InvalidOperationException(method.Name);
            }
        });
        using var instanceClient = new InstanceClient(async request =>
        {
            launches++;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/Instance/SpinUpServerInstance", request.RequestUri.AbsolutePath);
            Assert.Equal(Tenant.ToString(), Assert.Single(request.Headers.GetValues("X-CustomerGUID")));
            using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync());
            Assert.Equal(3, body.RootElement.Field("WorldServerID").GetInt32());
            Assert.Equal(17, body.RootElement.Field("ZoneInstanceID").GetInt32());
            Assert.Equal("UnrealDungeon", body.RootElement.Field("ZoneName").GetString());
            Assert.Equal(7777, body.RootElement.Field("Port").GetInt32());
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        host.Add<IHttpClientFactory>(instanceClient);

        await host.VerifyAsync(ApiService.Public, "POST", Users + "GetServerToConnectTo",
            new { UserSessionGUID = Session, CharacterName = Player, ZoneName = requestedZone, PlayerGroupType = group }, reply =>
            {
                reply.Is(expected);
                if (expected == 200)
                {
                    reply.Success();
                    var server = reply.Json();
                    Assert.Equal(17, server.Field("MapInstanceID").GetInt32());
                    Assert.Equal(status, server.Field("MapInstanceStatus").GetInt32());
                    Assert.Equal("192.0.2.44", server.Field("ServerIP").GetString());
                    Assert.Equal(7777, server.Field("Port").GetInt32());
                }
                Assert.Equal(status == 2 ? 1 : 0, assignments);
                Assert.Equal(spinUp ? 1 : 0, launches);
                assignments = launches = 0;
            });
    }

    [Theory]
    [InlineData(false, "ZoneName is NULL or Empty")]
    [InlineData(true, "WorldServerID is less than 1")]
    public async Task ZoneTravelReportsWhenNoDestinationCanBeAssigned(bool hasSavedZone, string error)
    {
        int joins = 0;
        var host = PlayerHost(characterCall: (method, args) =>
        {
            if (method.Name == nameof(ICharactersRepository.GetCharByCharName))
            {
                Assert.Equal(new object[] { Tenant, Player }, args);
                var character = PlayerCharacter();
                character.MapName = hasSavedZone ? "Harbor" : null;
                return Task.FromResult(character);
            }
            Assert.Equal(nameof(ICharactersRepository.JoinMapByCharacterId), method.Name);
            Assert.Equal(new object[] { Tenant, 42, Owner, "Harbor", 0 }, args);
            joins++;
            return Task.FromResult(new JoinMapByCharName { WorldServerID = 0 });
        });

        await host.VerifyAsync(ApiService.Public, "POST", Users + "GetServerToConnectTo",
            new { UserSessionGUID = Session, CharacterName = Player, ZoneName = "GETLASTZONENAME", PlayerGroupType = 0 }, reply =>
            {
                var json = reply.Json();
                Assert.False(json.Field("Success").GetBoolean());
                Assert.Contains(error, json.Field("ErrorMessage").GetString());
                Assert.Equal(hasSavedZone ? 1 : 0, joins);
                joins = 0;
            });
    }

    [Theory]
    [InlineData(17, 1, 200)]
    [InlineData(17, 2, 200)]
    [InlineData(17, 2, 404)]
    [InlineData(17, null, 404)]
    [InlineData(0, null, 400)]
    public async Task ZoneTravelPollingAssignsOnlyOnceTheInstanceIsReady(int mapInstanceId, int? status, int expected)
    {
        int assignments = 0;
        var host = PlayerHost(characterCall: (method, args) =>
        {
            switch (method.Name)
            {
                case nameof(ICharactersRepository.GetCharByCharName):
                    return Task.FromResult(PlayerCharacter());
                case nameof(ICharactersRepository.CheckMapInstanceStatus):
                    Assert.Equal(new object[] { Tenant, 17 }, args);
                    return Task.FromResult(status.HasValue ? new MapInstances { Status = status.Value } : null);
                case nameof(ICharactersRepository.AddOwnedCharacterToMapInstance):
                    Assert.Equal(new object[] { Tenant, 42, Owner, 17 }, args);
                    assignments++;
                    return Task.FromResult(expected == 200);
                default:
                    throw new InvalidOperationException(method.Name);
            }
        });
        await host.VerifyAsync(ApiService.Public, "POST", Users + "GetServerToConnectToStatus",
            new { UserSessionGUID = Session, CharacterName = Player, MapInstanceID = mapInstanceId }, reply =>
            {
                reply.Is(expected);
                if (expected == 200)
                {
                    reply.Success();
                    Assert.Equal(status, reply.Json().Field("MapInstanceStatus").GetInt32());
                }
                Assert.Equal(status == 2 ? 1 : 0, assignments);
                assignments = 0;
            });
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("5fce2a21-7527-4a5d-aad5-e7ab26467cb8")]
    public async Task PublicRoutesRejectMissingMalformedAndUnknownTenantHeaders(string tenant)
    {
        var host = new ApiTestHost();
        using var server = host.Start(ApiService.Public);
        var body = new { Email, Password };
        (await SendAsync(server, "POST", Users + "LoginAndCreateSession", body, tenant)).Is(401);
        (await host.SendLambdaAsync(ApiService.Public, "POST", Users + "LoginAndCreateSession", body, tenant)).Is(401);
    }

    [Theory]
    [InlineData("/api/Users/GetServerToConnectTo", "GET", "{}", 405)]
    [InlineData("/api/Users/GetServerToConnectToStatus", "POST", "{", 400)]
    [InlineData("/api/Users/GetServerToConnectToStatus", "POST", "null", 400)]
    [InlineData("/api/Characters/ByName", "POST", """{"UserSessionGUID":"invalid","CharacterName":"Aster"}""", 400)]
    public async Task InvalidTravelOrCharacterRequestsStopBeforeStorage(string path, string method, string body, int status)
    {
        var host = new ApiTestHost();
        await host.VerifyAsync(ApiService.Public, method, path, body, reply => reply.Is(status));
    }

    internal static ApiTestHost PlayerHost(Func<MethodInfo, object[], object> userCall = null,
        Func<MethodInfo, object[], object> characterCall = null, Func<GetUserSession> session = null)
    {
        var host = new ApiTestHost();
        host.Add<IUsersRepository>((method, args) =>
        {
            if (method.Name == nameof(IUsersRepository.CustomerExists))
                return Task.FromResult(Tenant.Equals(Assert.Single(args)));
            if (method.Name == nameof(IUsersRepository.GetUserSession))
            {
                Assert.Equal(new object[] { Tenant, Session }, args);
                return Task.FromResult(session == null ? PlayerSession() : session());
            }
            return userCall?.Invoke(method, args) ?? throw new InvalidOperationException(method.Name);
        });
        host.Add<ICharactersRepository>((method, args) =>
        {
            if (characterCall != null)
                return characterCall(method, args);
            Assert.Equal(nameof(ICharactersRepository.GetCharByCharName), method.Name);
            Assert.Equal(new object[] { Tenant, Player }, args);
            return Task.FromResult(PlayerCharacter());
        });
        return host;
    }

    internal static GetUserSession PlayerSession(string selected = Player) => new()
    {
        CustomerGuid = Tenant, UserGuid = Owner, UserSessionGUID = Session, CharacterID = 42,
        SelectedCharacterName = selected, CharName = Player, ZoneName = "Harbor", X = 12
    };

    private static GetCharByCharName PlayerCharacter(Guid? owner = null) => new()
    {
        CustomerGuid = Tenant, CharacterId = 42, CharName = Player, UserGuid = owner ?? Owner, MapName = "Harbor"
    };

    private static void CheckLogin(ApiReply reply, bool authenticated)
    {
        var json = reply.Json();
        Assert.Equal(authenticated, json.Field("Authenticated").GetBoolean());
        Assert.Equal(authenticated ? Session : Guid.Empty, json.Field("UserSessionGuid").GetGuid());
    }

    private static void VerifySelectedSession(ApiReply reply)
    {
        var session = reply.Json();
        Assert.Equal(Session, session.Field("UserSessionGUID").GetGuid());
        Assert.Equal(Player, session.Field("SelectedCharacterName").GetString());
        Assert.Equal("Harbor", session.Field("ZoneName").GetString());
        Assert.Equal(12, session.Field("X").GetDouble());
    }
}

internal sealed class InstanceClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
    : HttpMessageHandler, IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        Assert.Equal("OWSInstanceManagement", name);
        return new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("https://instance.example.test") };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        send(request);
}
