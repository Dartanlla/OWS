using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using OWSData.Models.StoredProcs;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using Xunit;
using static OWSTests.Api.ApiTestHost;

namespace OWSTests.Api;

[Collection(ApiTestCollection.Name)]
public class PersistenceApiTests
{
    private const string Player = "Aster";
    private const string Ability = "Fireball";
    private const string CustomJson = """{"charges":2}""";

    [Theory]
    [InlineData("/api/Characters/GetByName")]
    [InlineData("/api/Status/GetCharacterStatuses")]
    public async Task CharacterLookupReturnsGameState(string path)
    {
        var host = new ApiTestHost();
        host.Add<ICharactersRepository>((method, args) =>
        {
            Assert.Equal(nameof(ICharactersRepository.GetCharByCharName), method.Name);
            Assert.Equal(new object[] { Tenant, Player }, args);
            return Task.FromResult(new GetCharByCharName { CharacterId = 42, CharName = Player, MapName = "Harbor", X = -16 });
        });

        await host.VerifyAsync(ApiService.Persistence, "POST", path, new { CharacterName = Player }, reply =>
        {
            var character = reply.Json();
            Assert.Equal(42, character.Field("CharacterId").GetInt32());
            Assert.Equal(Player, character.GetProperty("CharacterName").GetString());
            Assert.Equal("Harbor", character.Field("MapName").GetString());
            Assert.Equal(-16, character.Field("X").GetSingle());
        });
    }

    [Fact]
    public async Task CharacterIdRouteReturnsCharacterAndCustomFields()
    {
        var host = new ApiTestHost();
        host.Add<ICharactersRepository>((method, args) => method.Name switch
        {
            nameof(ICharactersRepository.GetCharByCharId) => LookupCharacter(args),
            nameof(ICharactersRepository.GetCustomCharacterData) => ReadCustomData(args),
            _ => throw new InvalidOperationException(method.Name)
        });

        await host.VerifyAsync(ApiService.Persistence, "POST", "/api/Characters/42", null, reply =>
        {
            var json = reply.Json();
            Assert.Equal(42, json.Field("CharacterData").Field("CharacterId").GetInt32());
            Assert.Equal("blue", json.Field("CustomCharacterDataRows")[0].Field("FieldValue").GetString());
        });
    }

    [Fact]
    public async Task ReadsAndUpdatesCustomCharacterData()
    {
        var host = new ApiTestHost();
        string color = "blue";
        host.Add<ICharactersRepository>((method, args) =>
        {
            Assert.Equal(Tenant, args[0]);
            if (method.Name == nameof(ICharactersRepository.AddOrUpdateCustomCharacterData))
            {
                var data = Assert.IsType<AddOrUpdateCustomCharacterData>(args[1]);
                Assert.Equal(Player, data.CharacterName);
                Assert.Equal("HairColor", data.CustomFieldName);
                color = data.FieldValue;
                return Task.CompletedTask;
            }

            Assert.Equal(nameof(ICharactersRepository.GetCustomCharacterData), method.Name);
            Assert.Equal(Player, args[1]);
            return Task.FromResult<IEnumerable<CustomCharacterData>>(
                [new() { CustomFieldName = "HairColor", FieldValue = color }]);
        });

        await host.VerifyAsync(ApiService.Persistence, "POST", "/api/Characters/GetCustomData",
            new { CharacterName = Player }, reply => Assert.Equal("blue", reply.Json().Field("Rows")[0].Field("FieldValue").GetString()));

        const string updateRoute = "/api/Characters/AddOrUpdateCustomData";
        object Update(string value) => new
        {
            AddOrUpdateCustomCharacterData = new { CharacterName = Player, CustomFieldName = "HairColor", FieldValue = value }
        };
        using var server = host.Start(ApiService.Persistence);
        (await SendAsync(server, "POST", updateRoute, Update("green"))).Is(200);
        Assert.Equal("green", color);
        Assert.Equal(JsonValueKind.Null, (await host.SendLambdaAsync(ApiService.Persistence, "POST", updateRoute, Update("purple"))).Json().ValueKind);
        Assert.Equal("purple", color);

        await host.VerifyAsync(ApiService.Persistence, "POST", "/api/Characters/GetCustomData",
            new { CharacterName = Player }, reply => Assert.Equal("purple", reply.Json().Field("Rows")[0].Field("FieldValue").GetString()));
    }

    [Fact]
    public async Task SavesEveryPositionInThePluginBatch()
    {
        var host = new ApiTestHost();
        var writes = new List<object[]>();
        host.Add<ICharactersRepository>((method, args) =>
        {
            Assert.Equal(nameof(ICharactersRepository.UpdatePosition), method.Name);
            writes.Add(args);
            return Task.CompletedTask;
        });

        await host.VerifyAsync(ApiService.Persistence, "POST", "/api/Characters/UpdateAllPlayerPositions",
            new { MapName = "Harbor", SerializedPlayerLocationData = "Aster:11:-22:33:4:5:6|Briar:-41:52:63:7:8:9" },
            reply =>
            {
                reply.Success();
                Assert.Collection(writes,
                    first => Assert.Equal(new object[] { Tenant, Player, "Harbor", 11f, -22f, 33f, 4f, 5f, 6f }, first),
                    second => Assert.Equal(new object[] { Tenant, "Briar", "Harbor", -41f, 52f, 63f, 7f, 8f, 9f }, second));
                writes.Clear();
            });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavingCharacterStatsReportsWriteOutcome(bool writeFails)
    {
        var host = new ApiTestHost();
        int writes = 0;
        host.Add<ICharactersRepository>((method, args) =>
        {
            Assert.Equal(nameof(ICharactersRepository.UpdateCharacterStats), method.Name);
            var stats = Assert.IsType<UpdateCharacterStats>(Assert.Single(args));
            Assert.Equal(Player, stats.CharName);
            Assert.Equal(Tenant.ToString(), stats.CustomerGUID);
            Assert.Equal(-12.5f, stats.X);
            Assert.Equal(90, stats.RZ);
            writes++;
            return writeFails ? Task.FromException(new InvalidOperationException("Storage unavailable")) : Task.CompletedTask;
        });

        await host.VerifyAsync(ApiService.Persistence, "POST", "/api/Characters/UpdateCharacterStats",
            new { UpdateCharacterStats = new { CharName = Player, CustomerGUID = Tenant, X = -12.5f, RZ = 90 } }, reply =>
            {
                var result = reply.Json();
                Assert.Equal(!writeFails, result.Field("Success").GetBoolean());
                if (writeFails)
                    Assert.Equal("Storage unavailable", result.Field("ErrorMessage").GetString());
                Assert.Equal(1, writes);
                writes = 0;
            });
    }

    [Fact]
    public async Task PlayerLogoutPersistsTheExactCharacter()
    {
        var host = new ApiTestHost();
        int logouts = 0;
        host.Add<ICharactersRepository>((method, args) =>
        {
            Assert.Equal(nameof(ICharactersRepository.PlayerLogout), method.Name);
            Assert.Equal(new object[] { Tenant, Player }, args);
            logouts++;
            return Task.CompletedTask;
        });
        await host.VerifyAsync(ApiService.Persistence, "POST", "/api/Characters/PlayerLogout",
            new { CharacterName = Player }, reply => reply.Success());
        Assert.Equal(2, logouts);
    }

    [Theory]
    [InlineData("AddAbilityToCharacter", 2)]
    [InlineData("UpdateAbilityOnCharacter", 3)]
    [InlineData("RemoveAbilityFromCharacter", null)]
    public async Task AbilityMutationsForwardPluginFields(string action, int? level)
    {
        var host = new ApiTestHost();
        int writes = 0;
        host.Add<ICharactersRepository>((method, args) =>
        {
            Assert.Equal(action, method.Name);
            object[] expected = level.HasValue ? [Tenant, Ability, Player, level.Value, CustomJson] : [Tenant, Ability, Player];
            Assert.Equal(expected, args);
            writes++;
            return Task.CompletedTask;
        });

        var body = level.HasValue
            ? (object)new { CharacterName = Player, AbilityName = Ability, AbilityLevel = level, CharHasAbilitiesCustomJSON = CustomJson }
            : new { CharacterName = Player, AbilityName = Ability };
        await host.VerifyAsync(ApiService.Persistence, "POST", "/api/Abilities/" + action, body, reply => reply.Success());
        Assert.Equal(2, writes);
    }

    [Theory]
    [InlineData("GetAbilities", "GET", false)]
    [InlineData("GetCharacterAbilities", "POST", false)]
    [InlineData("GetAbilityBars", "POST", false)]
    [InlineData("GetAbilities", "GET", true)]
    [InlineData("GetCharacterAbilities", "POST", true)]
    [InlineData("GetAbilityBars", "POST", true)]
    public async Task AbilityListsHaveTheShapeConsumedByTheClient(string action, string verb, bool empty)
    {
        var host = new ApiTestHost();
        host.Add<ICharactersRepository>((method, args) =>
        {
            Assert.Equal(action, method.Name);
            Assert.Equal(verb == "GET" ? new object[] { Tenant } : [Tenant, Player], args);
            return action switch
            {
                "GetAbilities" => Task.FromResult<IEnumerable<Abilities>>(empty ? [] : [new() { AbilityName = Ability }]),
                "GetCharacterAbilities" => Task.FromResult<IEnumerable<GetCharacterAbilities>>(
                    empty ? [] : [new() { AbilityName = Ability, AbilityLevel = 3, CharHasAbilitiesCustomJSON = CustomJson }]),
                "GetAbilityBars" => Task.FromResult<IEnumerable<GetAbilityBars>>(
                    empty ? [] : [new() { AbilityBarName = "Main", MaxNumberOfSlots = 8 }]),
                _ => throw new InvalidOperationException(action)
            };
        });

        await host.VerifyAsync(ApiService.Persistence, verb, "/api/Abilities/" + action,
            verb == "GET" ? null : new { CharacterName = Player }, reply =>
            {
                var rows = reply.Json();
                Assert.Equal(empty ? 0 : 1, rows.GetArrayLength());
                if (empty) return;
                if (action == "GetAbilityBars")
                {
                    Assert.Equal("Main", rows[0].Field("AbilityBarName").GetString());
                    Assert.Equal(8, rows[0].Field("MaxNumberOfSlots").GetInt32());
                }
                else
                {
                    Assert.Equal(Ability, rows[0].Field("AbilityName").GetString());
                    if (action == "GetCharacterAbilities")
                    {
                        Assert.Equal(3, rows[0].Field("AbilityLevel").GetInt32());
                        Assert.Equal(CustomJson, rows[0].Field("CharHasAbilitiesCustomJSON").GetString());
                    }
                }
            });
    }

    [Fact]
    public async Task CombinedAbilityBarsReturnsNestedAbilitiesInLambda()
    {
        var host = new ApiTestHost();
        host.Add<ICharactersRepository>((method, args) =>
        {
            Assert.Equal(nameof(ICharactersRepository.GetAbilityBarsAndAbilities), method.Name);
            Assert.Equal(new object[] { Tenant, Player }, args);
            return Task.FromResult<IEnumerable<GetAbilityBarsAndAbilities>>(
                [new() { AbilityBarName = "Main", Abilities = [new() { AbilityName = Ability }] }]);
        });
        var reply = await host.SendLambdaAsync(ApiService.Persistence, "POST", "/api/Abilities/GetAbilityBarsAndAbilities",
            new { CharacterName = Player });
        var bar = reply.Json()[0];
        Assert.Equal("Main", bar.Field("AbilityBarName").GetString());
        Assert.Equal(Ability, bar.Field("Abilities")[0].Field("AbilityName").GetString());
    }

    [Fact]
    public async Task CombinedAbilityBarsHttpRouteCurrentlyReturnsFlatCharacterAbilities()
    {
        // The HTTP controller is currently wired to GetCharacterAbilities.
        // Update this regression when HTTP and Lambda return the same shape.
        var host = new ApiTestHost();
        host.Add<ICharactersRepository>((method, args) =>
        {
            Assert.Equal(nameof(ICharactersRepository.GetCharacterAbilities), method.Name);
            Assert.Equal(new object[] { Tenant, Player }, args);
            return Task.FromResult<IEnumerable<GetCharacterAbilities>>([new() { AbilityName = Ability, AbilityLevel = 3 }]);
        });
        using var server = host.Start(ApiService.Persistence);
        var reply = await SendAsync(server, "POST", "/api/Abilities/GetAbilityBarsAndAbilities", new { CharacterName = Player });
        var ability = Assert.Single(reply.Json().EnumerateArray());
        Assert.Equal(Ability, ability.Field("AbilityName").GetString());
        Assert.Equal(3, ability.Field("AbilityLevel").GetInt32());
    }

    private static Task<GetCharByCharName> LookupCharacter(object[] args)
    {
        Assert.Equal(new object[] { Tenant, 42 }, args);
        return Task.FromResult(new GetCharByCharName { CharacterId = 42, CharName = Player });
    }

    private static Task<IEnumerable<CustomCharacterData>> ReadCustomData(object[] args)
    {
        Assert.Equal(new object[] { Tenant, Player }, args);
        return Task.FromResult<IEnumerable<CustomCharacterData>>([new() { CustomFieldName = "HairColor", FieldValue = "blue" }]);
    }
}
