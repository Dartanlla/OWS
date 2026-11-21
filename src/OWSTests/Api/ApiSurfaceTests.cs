using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static OWSTests.Api.ApiTestHost;

namespace OWSTests.Api;

[Collection(ApiTestCollection.Name)]
public class ApiSurfaceTests
{
    [Theory]
    [InlineData(ApiService.Public, """
        GET /api/System/Status
        GET /api/Users/GetUserSession
        POST /api/Characters/ByName
        POST /api/Characters/GetDefaultCustomData
        POST /api/Characters/{characterId:int}
        POST /api/Users/CreateCharacter
        POST /api/Users/CreateCharacterUsingDefaultCharacterValues
        POST /api/Users/ExternalConnectionTicket
        POST /api/Users/ExternalLoginAndCreateSession
        POST /api/Users/GetAllCharacters
        POST /api/Users/GetPlayerGroupsCharacterIsIn
        POST /api/Users/GetServerToConnectTo
        POST /api/Users/GetServerToConnectToStatus
        POST /api/Users/LoginAndCreateSession
        POST /api/Users/Logout
        POST /api/Users/RegisterUser
        POST /api/Users/RemoveCharacter
        POST /api/Users/SetSelectedCharacterAndGetUserSession
        POST /api/Users/UserSessionSetSelectedCharacter
        """)]
    [InlineData(ApiService.Persistence, """
        GET /api/Abilities/GetAbilities
        GET /api/System/Status
        POST /api/Abilities/AddAbilityToCharacter
        POST /api/Abilities/GetAbilityBars
        POST /api/Abilities/GetAbilityBarsAndAbilities
        POST /api/Abilities/GetCharacterAbilities
        POST /api/Abilities/RemoveAbilityFromCharacter
        POST /api/Abilities/UpdateAbilityOnCharacter
        POST /api/Characters/AddOrUpdateCustomData
        POST /api/Characters/GetByName
        POST /api/Characters/GetCustomData
        POST /api/Characters/PlayerLogout
        POST /api/Characters/UpdateAllPlayerPositions
        POST /api/Characters/UpdateCharacterStats
        POST /api/Characters/{characterId:int}
        POST /api/Status/GetCharacterStatuses
        """)]
    [InlineData(ApiService.Instance, """
        GET /api/Instance/StartInstanceLauncher
        GET /api/System/Status
        POST /api/Instance/AddEdgeServer
        POST /api/Instance/AddMapInstance
        POST /api/Instance/CheckReadyAssignment
        POST /api/Instance/DeleteEdgeServer
        POST /api/Instance/GetCurrentWorldTime
        POST /api/Instance/GetEdgeServer
        POST /api/Instance/GetEdgeServers
        POST /api/Instance/GetReadyMapInstances
        POST /api/Instance/GetServerInstanceFromPort
        POST /api/Instance/GetServerToConnectTo
        POST /api/Instance/GetZoneInstance
        POST /api/Instance/GetZoneInstancesForWorldServer
        POST /api/Instance/GetZoneInstancesForZone
        POST /api/Instance/ProvisionEdgeServerCredential
        POST /api/Instance/RedeemExternalConnectionTicket
        POST /api/Instance/RegisterLauncher
        POST /api/Instance/RevokeEdgeServerCredential
        POST /api/Instance/SetEdgeServerReady
        POST /api/Instance/SetZoneInstanceStatus
        POST /api/Instance/ShutDownInstanceLauncher
        POST /api/Instance/ShutDownServerInstance
        POST /api/Instance/SpinUpServerInstance
        POST /api/Instance/UpdateEdgeServer
        POST /api/Instance/UpdateNumberOfPlayers
        POST /api/Zones/AddZone
        """)]
    [InlineData(ApiService.GlobalData, """
        GET /api/GlobalData/GetGlobalDataItem/{globalDataKey}
        GET /api/System/Status
        POST /api/GlobalData/AddOrUpdateGlobalDataItem
        """)]
    [InlineData(ApiService.Content, """
        GET /api/Content/GetContentFeature
        GET /api/Content/GetContentPackage
        GET /api/ContentCache/GetContentCache
        GET /api/Example/Get
        POST /api/Content/AddOrUpdateContentFeature
        POST /api/Content/AddOrUpdateContentPackage
        POST /api/ContentCache/AddOrUpdateContentCache
        POST /api/Example/Post
        """)]
    [InlineData(ApiService.Management, """
        GET /api/System/Status
        GET /api/Users
        POST /api/Users
        PUT /api/Users
        """)]
    public void RouteInventoryFlagsNewRemovedOrRenamedEndpoints(ApiService service, string routeInventory)
    {
        using var server = new ApiTestHost().Start(service);
        var routes = server.Services.GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
            .SelectMany(action =>
            {
                var route = "/" + action.AttributeRouteInfo.Template.Trim('/');
                var verbs = action.ActionConstraints?.OfType<HttpMethodActionConstraint>()
                    .SelectMany(constraint => constraint.HttpMethods).ToArray() ?? [];
                return verbs.DefaultIfEmpty("*").Select(verb => verb + " " + route);
            })
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();

        var expected = routeInventory.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(route => route, StringComparer.Ordinal);
        Assert.Equal(expected, routes);
    }

    [Theory]
    [InlineData(ApiService.Public, true)]
    [InlineData(ApiService.Persistence, true)]
    [InlineData(ApiService.Instance, true)]
    [InlineData(ApiService.GlobalData, false)]
    [InlineData(ApiService.Management, false)]
    public async Task HealthCheckReportsReadyWithoutATenantOrStorage(ApiService service, bool hasLambda)
    {
        const string path = "/api/System/Status";
        var host = new ApiTestHost();
        using var server = host.Start(service);
        Assert.True((await SendAsync(server, "GET", path, tenant: "")).Json().GetBoolean());
        if (hasLambda)
            Assert.True((await host.SendLambdaAsync(service, "GET", path, tenant: "")).Json().GetBoolean());
    }

    [Theory]
    [InlineData(ApiService.Public)]
    [InlineData(ApiService.Persistence)]
    [InlineData(ApiService.Instance)]
    public async Task UnknownActionReturnsNotFoundInBothHosts(ApiService service)
    {
        var host = new ApiTestHost();
        using var server = host.Start(service);
        const string path = "/api/System/UnknownAction";
        (await SendAsync(server, "GET", path)).Is(404);
        var lambda = await host.SendLambdaAsync(service, "GET", path);
        Assert.NotEmpty(lambda.Json(404).Field("ErrorMessage").GetString());
    }
}
