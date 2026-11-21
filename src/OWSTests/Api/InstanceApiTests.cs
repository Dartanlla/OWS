using System;
using System.Collections.Generic;
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
public class InstanceApiTests
{
    private const string Instance = "/api/Instance/";
    private const string Launcher = "7baa06b5-52fd-4f82-ba62-5f7957dcaa12";
    private static readonly SuccessAndErrorMessage Success = new() { Success = true, ErrorMessage = "" };

    [Theory]
    [InlineData("GetZoneInstance")]
    [InlineData("GetServerInstanceFromPort")]
    public async Task ZoneInstanceCanBeResolvedByIdOrSourceIpAndPort(string action)
    {
        var host = new ApiTestHost();
        host.Add<IInstanceManagementRepository>((method, args) =>
        {
            Assert.Equal(action, method.Name);
            Assert.Equal(action == "GetZoneInstance" ? new object[] { Tenant, 17 } : [Tenant, RemoteIp, 7777], args);
            return Task.FromResult(new GetServerInstanceFromPort
            {
                MapInstanceID = 17, ZoneName = "Harbor", MapName = "UnrealHarbor", Status = 2,
                WorldCompContainsFilter = "Harbor", WorldCompListFilter = "Docks,Market"
            });
        });

        object body = action == "GetZoneInstance" ? 17 : new { Port = 7777 };
        await host.VerifyAsync(ApiService.Instance, "POST", Instance + action, body, reply =>
        {
            var instance = reply.Json();
            Assert.Equal(17, instance.Field("MapInstanceID").GetInt32());
            Assert.Equal("Harbor", instance.Field("ZoneName").GetString());
            Assert.Equal("UnrealHarbor", instance.Field("MapName").GetString());
            Assert.Equal(2, instance.Field("Status").GetInt32());
            Assert.Equal("Docks,Market", instance.Field("WorldCompListFilter").GetString());
        });
    }

    [Fact]
    public async Task Ue5ZoneInstanceLookupObjectIsCurrentlyRejected()
    {
        // AOWSGameMode sends an object, while both API entry points bind a bare JSON integer.
        var host = new ApiTestHost();
        using var server = host.Start(ApiService.Instance);
        const string path = Instance + "GetZoneInstance";
        const string body = """{"ZoneInstanceId":17}""";
        (await SendAsync(server, "POST", path, body)).Is(400);
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() =>
            host.SendLambdaAsync(ApiService.Instance, "POST", path, body));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ZoneInstanceListsKeepThePluginFilterAndPlayerCounts(bool byZone)
    {
        var host = new ApiTestHost();
        host.Add<IInstanceManagementRepository>((method, args) =>
        {
            if (byZone)
            {
                Assert.Equal(nameof(IInstanceManagementRepository.GetZoneInstancesOfZone), method.Name);
                Assert.Equal(new object[] { Tenant, "Harbor" }, args);
                return Task.FromResult<IEnumerable<GetZoneInstancesForZone>>(
                    [new() { MapInstanceID = 17, WorldServerID = 3, Port = 7777, NumberOfReportedPlayers = 6 }]);
            }

            Assert.Equal(nameof(IInstanceManagementRepository.GetZoneInstancesForWorldServer), method.Name);
            Assert.Equal(new object[] { Tenant, 3 }, args);
            return Task.FromResult<IEnumerable<GetZoneInstancesForWorldServer>>(
                [new() { MapInstanceID = 17, WorldServerID = 3, Port = 7777, NumberOfReportedPlayers = 6 }]);
        });
        object body = byZone ? new { Request = new { ZoneName = "Harbor" } } : new { Request = new { WorldServerID = 3 } };
        var path = Instance + (byZone ? "GetZoneInstancesForZone" : "GetZoneInstancesForWorldServer");
        await host.VerifyAsync(ApiService.Instance, "POST", path, body, reply =>
        {
            var instance = Assert.Single(reply.Json().EnumerateArray());
            Assert.Equal(17, instance.Field("MapInstanceID").GetInt32());
            Assert.Equal(3, instance.Field("WorldServerID").GetInt32());
            Assert.Equal(7777, instance.Field("Port").GetInt32());
            Assert.Equal(6, instance.Field("NumberOfReportedPlayers").GetInt32());
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task PlayerCountUpdatesIncludeAnEmptyServer(int players)
    {
        var host = new ApiTestHost();
        int writes = 0;
        host.Add<IInstanceManagementRepository>((method, args) =>
        {
            Assert.Equal(nameof(IInstanceManagementRepository.UpdateNumberOfPlayers), method.Name);
            Assert.Equal(new object[] { Tenant, 17, players }, args);
            writes++;
            return Task.FromResult(Success);
        });
        await host.VerifyAsync(ApiService.Instance, "POST", Instance + "UpdateNumberOfPlayers",
            new { ZoneInstanceId = 17, NumberOfConnectedPlayers = players }, reply => reply.Success());
        Assert.Equal(2, writes);
    }

    [Fact]
    public async Task WorldTimePreservesA64BitTimestamp()
    {
        const long worldTime = 6_400_100_000_123;
        var host = new ApiTestHost();
        host.Add<IInstanceManagementRepository>((method, args) =>
        {
            Assert.Equal(nameof(IInstanceManagementRepository.GetCurrentWorldTime), method.Name);
            Assert.Equal(Tenant, Assert.Single(args));
            return Task.FromResult(new GetCurrentWorldTime { CurrentWorldTime = worldTime });
        });
        await host.VerifyAsync(ApiService.Instance, "POST", Instance + "GetCurrentWorldTime", new { },
            reply => Assert.Equal(worldTime, reply.Json().Field("CurrentWorldTime").GetInt64()));
    }

    [Fact]
    public async Task AddingAZoneForwardsCapsModeAndWorldCompositionFilters()
    {
        var host = new ApiTestHost();
        int writes = 0;
        host.Add<IInstanceManagementRepository>((method, args) =>
        {
            Assert.Equal(nameof(IInstanceManagementRepository.AddZone), method.Name);
            Assert.Equal(new object[] { Tenant, "UnrealHarbor", "Harbor", "Harbor", "Docks,Market", 12, 24, 1 }, args);
            writes++;
            return Task.FromResult(Success);
        });
        await host.VerifyAsync(ApiService.Instance, "POST", "/api/Zones/AddZone", new
        {
            AddOrUpdateZone = new
            {
                MapName = "UnrealHarbor", ZoneName = "Harbor", WorldCompContainsFilter = "Harbor",
                WorldCompListFilter = "Docks,Market", SoftPlayerCap = 12, HardPlayerCap = 24, MapMode = 1
            }
        }, reply => reply.Success());
        Assert.Equal(2, writes);
    }

    [Theory]
    [InlineData("RegisterLauncher")]
    [InlineData("StartInstanceLauncher")]
    [InlineData("ShutDownInstanceLauncher")]
    [InlineData("SetZoneInstanceStatus")]
    public async Task LauncherLifecycleUsesTheCorrectTenantWorldAndInstance(string action)
    {
        var host = new ApiTestHost();
        int calls = 0;
        host.Add<IInstanceManagementRepository>((method, args) =>
        {
            calls++;
            switch (action)
            {
                case "RegisterLauncher":
                    Assert.Equal(nameof(IInstanceManagementRepository.RegisterLauncher), method.Name);
                    Assert.Equal(new object[] { Tenant, Launcher, "192.0.2.1", 25, "10.0.0.2", 7777 }, args);
                    break;
                case "StartInstanceLauncher":
                    Assert.Equal(nameof(IInstanceManagementRepository.StartWorldServer), method.Name);
                    Assert.Equal(new object[] { Tenant, Launcher }, args);
                    return Task.FromResult(3);
                case "ShutDownInstanceLauncher":
                    Assert.Equal(nameof(IInstanceManagementRepository.ShutDownWorldServer), method.Name);
                    Assert.Equal(new object[] { Tenant, 3 }, args);
                    break;
                case "SetZoneInstanceStatus":
                    Assert.Equal(nameof(IInstanceManagementRepository.SetZoneInstanceStatus), method.Name);
                    Assert.Equal(new object[] { Tenant, 17, 2 }, args);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
            return Task.FromResult(Success);
        });
        object body = action switch
        {
            "RegisterLauncher" => new
            {
                Request = new
                {
                    LauncherGuid = Launcher, ServerIP = "192.0.2.1", MaxNumberOfInstances = 25,
                    InternalServerIP = "10.0.0.2", StartingInstancePort = 7777
                }
            },
            "ShutDownInstanceLauncher" => new { Request = new { WorldServerID = 3 } },
            "SetZoneInstanceStatus" => new { Request = new { ZoneInstanceID = 17, InstanceStatus = 2 } },
            _ => null
        };
        var headers = new Dictionary<string, string> { ["X-LauncherGUID"] = Launcher };
        var verb = action == "StartInstanceLauncher" ? "GET" : "POST";
        await host.VerifyAsync(ApiService.Instance, verb, Instance + action, body, reply =>
        {
            if (verb == "GET") Assert.Equal(3, reply.Json().GetInt32());
            else reply.Success();
        }, headers);
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InstanceLifecyclePublishesOneMessageToTheCorrectWorldServer(bool spinUp)
    {
        var host = new ApiTestHost();
        int messages = 0;
        host.Add<IInstanceLauncherMessaging>((method, args) =>
        {
            Assert.Equal(spinUp ? "PublishSpinUpServerMessage" : "PublishShutDownServerMessage", method.Name);
            Assert.Equal(spinUp ? new object[] { Tenant, 3, 17, "UnrealHarbor", 7777 } : [Tenant, 3, 17], args);
            messages++;
            return null;
        });
        var action = spinUp ? "SpinUpServerInstance" : "ShutDownServerInstance";
        await host.VerifyAsync(ApiService.Instance, "POST", Instance + action,
            new { WorldServerID = 3, ZoneInstanceID = 17, ZoneName = "UnrealHarbor", Port = 7777 }, reply =>
            {
                reply.Success();
                Assert.Equal(1, messages);
                messages = 0;
            });
    }

    [Theory]
    [InlineData("Harbor", 2)]
    [InlineData("GETLASTZONENAME", 1)]
    public async Task InternalServerLookupReturnsTheAssignedInstance(string requestedZone, int status)
    {
        var host = new ApiTestHost();
        int polls = 0;
        host.Add<ICharactersRepository>((method, args) =>
        {
            switch (method.Name)
            {
                case nameof(ICharactersRepository.GetCharByCharName):
                    Assert.Equal("GETLASTZONENAME", requestedZone);
                    Assert.Equal(new object[] { Tenant, "Aster" }, args);
                    return Task.FromResult(new GetCharByCharName { MapName = "Harbor" });
                case nameof(ICharactersRepository.JoinMapByCharName):
                    Assert.Equal(new object[] { Tenant, "Aster", "Harbor", 4 }, args);
                    return Task.FromResult(new JoinMapByCharName { MapInstanceID = 17, MapInstanceStatus = status, Port = 7777 });
                case nameof(ICharactersRepository.CheckMapInstanceStatus):
                    Assert.Equal(new object[] { Tenant, 17 }, args);
                    polls++;
                    return Task.FromResult(new MapInstances { Status = 2 });
                default:
                    throw new InvalidOperationException(method.Name);
            }
        });
        await host.VerifyAsync(ApiService.Instance, "POST", Instance + "GetServerToConnectTo",
            new { CharacterName = "Aster", ZoneName = requestedZone, PlayerGroupType = 4 }, reply =>
            {
                var result = reply.Json();
                Assert.Equal(17, result.Field("MapInstanceID").GetInt32());
                Assert.Equal(7777, result.Field("Port").GetInt32());
                Assert.Equal(status == 1 ? 1 : 0, polls);
                polls = 0;
            });
    }
}
