using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWSData.Models;
using OWSData.Models.Tables;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;
using Xunit;
using static OWSTests.Api.ApiTestHost;

namespace OWSTests.Api;

[Collection(ApiTestCollection.Name)]
public class EdgeServerApiTests
{
    private const string LauncherCredential = "example-regression-launcher-credential";
    private const string Endpoint = "edge.example.test";
    private const int Id = 17;
    private const int Port = 7777;
    private const string EdgeCredential = "example-regression-edge-credential";

    [Theory]
    [InlineData("ProvisionEdgeServerCredential", "launcher", true, true, 200)]
    [InlineData("ProvisionEdgeServerCredential", "launcher", false, true, 400)]
    [InlineData("ProvisionEdgeServerCredential", "launcher", true, false, 409)]
    [InlineData("ProvisionEdgeServerCredential", "edge", true, true, 401)]
    [InlineData("SetEdgeServerReady", "edge", true, true, 200)]
    [InlineData("SetEdgeServerReady", "edge", true, false, 401)]
    [InlineData("SetEdgeServerReady", "launcher", true, true, 401)]
    [InlineData("RevokeEdgeServerCredential", "edge", true, true, 200)]
    [InlineData("RevokeEdgeServerCredential", "edge", true, false, 401)]
    [InlineData("RevokeEdgeServerCredential", "launcher", true, true, 200)]
    [InlineData("RevokeEdgeServerCredential", "launcher", true, false, 404)]
    public async Task CredentialsAreManagedByEdgeId(string operation, string credential, bool validHash, bool updated, int status)
    {
        using var launcher = new EnvironmentSetting(ServerCredentials.LauncherVariable, LauncherCredential);
        var hash = ServerCredentials.Hash(EdgeCredential);
        var host = new ApiTestHost();
        var calls = 0;
        host.Add<IInstanceManagementRepository>((method, args) =>
        {
            calls++;
            Assert.Equal(operation, method.Name);
            var expectedHash = operation == "RevokeEdgeServerCredential" && credential == "launcher" ? null : hash;
            Assert.Equal(new object[] { Tenant, Id, expectedHash }, args);
            return Task.FromResult(updated);
        });
        var headers = credential == "launcher" ? LauncherHeaders() : new Dictionary<string, string>
        {
            [ServerCredentials.CredentialHeader] = EdgeCredential
        };

        await host.VerifyAsync(ApiService.Instance, "POST", "/api/Instance/" + operation,
            new { CustomerGuid = Tenant, EdgeServerId = Id, CredentialHash = validHash ? hash.ToLowerInvariant() : "invalid" },
            reply =>
            {
                var json = reply.Json(status);
                if (status == 200)
                    Assert.True(json.Field("Success").GetBoolean());
                else
                    Assert.NotEmpty(json.Field("ErrorMessage").GetString());
                Assert.Equal(status == 400 || (status == 401 && updated) ? 0 : 1, calls);
                calls = 0;
            }, headers);
    }

    private static Dictionary<string, string> LauncherHeaders() => new()
    {
        [ServerCredentials.LauncherHeader] = LauncherCredential
    };

    [Theory]
    [InlineData("AddEdgeServer", 201)]
    [InlineData("AddEdgeServer", 404)]
    [InlineData("AddEdgeServer", 409)]
    [InlineData("GetEdgeServer", 200)]
    [InlineData("GetEdgeServer", 404)]
    [InlineData("GetEdgeServers", 200)]
    [InlineData("UpdateEdgeServer", 200)]
    [InlineData("UpdateEdgeServer", 404)]
    [InlineData("UpdateEdgeServer", 409)]
    [InlineData("DeleteEdgeServer", 200)]
    [InlineData("DeleteEdgeServer", 404)]
    public async Task ManagesEdgeServersWithinTheRequestedCustomer(string action, int status)
    {
        using var launcher = new EnvironmentSetting("OWS_INSTANCE_LAUNCHER_CREDENTIAL", LauncherCredential);
        var server = new EdgeServers { CustomerGuid = Tenant, EdgeServerId = Id, Host = Endpoint, Port = Port, IsActive = true };
        var host = new ApiTestHost();
        var calls = 0;
        host.Add<IInstanceManagementRepository>((method, args) =>
        {
            Assert.Equal(action, method.Name);
            calls++;
            switch (action)
            {
                case "AddEdgeServer":
                    Assert.Equal(new object[] { Tenant, Endpoint, Port, false }, args);
                    if (status == 409)
                        throw new EdgeServerEndpointInUseException(null);
                    return Task.FromResult(status == 201 ? Id : 0);
                case "GetEdgeServer":
                    Assert.Equal(new object[] { Tenant, Id }, args);
                    return Task.FromResult(status == 200 ? server : null);
                case "GetEdgeServers":
                    Assert.Equal(new object[] { Tenant, true }, args);
                    return Task.FromResult<IEnumerable<EdgeServers>>([server]);
                case "UpdateEdgeServer":
                    Assert.Equal(new object[] { Tenant, Id, Endpoint, Port, true }, args);
                    if (status == 409)
                        throw new EdgeServerEndpointInUseException(null);
                    return Task.FromResult(status == 200);
                case "DeleteEdgeServer":
                    Assert.Equal(new object[] { Tenant, Id }, args);
                    return Task.FromResult(status == 200);
                default:
                    throw new InvalidOperationException(action);
            }
        });

        var request = new
        {
            CustomerGuid = Tenant,
            EdgeServerId = Id,
            Host = " EDGE.Example.Test. ",
            Port,
            IsActive = action == "AddEdgeServer" ? (bool?)null : true
        };
        await host.VerifyAsync(ApiService.Instance, "POST", "/api/Instance/" + action, request,
            reply =>
            {
                var json = reply.Json(status);
                if (status >= 400)
                    Assert.NotEmpty(json.Field("ErrorMessage").GetString());
                else if (action == "GetEdgeServers")
                {
                    var record = Assert.Single(json.EnumerateArray());
                    Assert.Equal(Id, record.Field("EdgeServerId").GetInt32());
                    Assert.DoesNotContain(record.EnumerateObject(), property => property.Name.Contains("Credential", StringComparison.OrdinalIgnoreCase));
                }
                else if (action is "AddEdgeServer" or "GetEdgeServer")
                {
                    Assert.Equal(Id, json.Field("EdgeServerId").GetInt32());
                    Assert.DoesNotContain(json.EnumerateObject(), property => property.Name.Contains("Credential", StringComparison.OrdinalIgnoreCase));
                }
                else
                    Assert.True(json.Field("Success").GetBoolean());
                Assert.Equal(1, calls);
                calls = 0;
            }, LauncherHeaders());
    }

    [Theory]
    [InlineData("AddEdgeServer", "edge.example.test", 7777, 17, true, false, 401)]
    [InlineData("AddEdgeServer", "https://edge.example.test", 7777, 17, true, true, 400)]
    [InlineData("AddEdgeServer", "edge.example.test", 65536, 17, true, true, 400)]
    [InlineData("GetEdgeServers", null, 0, 0, false, true, 400)]
    [InlineData("DeleteEdgeServer", null, 0, 0, true, true, 400)]
    [InlineData("UpdateEdgeServer", "edge.example.test", 7777, 17, true, true, 400)]
    public async Task InvalidRequestsDoNotReachStorage(string action, string hostName, int port, int edgeServerId,
        bool hasCustomer, bool hasCredential, int status)
    {
        using var launcher = new EnvironmentSetting("OWS_INSTANCE_LAUNCHER_CREDENTIAL", LauncherCredential);
        var host = new ApiTestHost();
        host.Add<IInstanceManagementRepository>((_, _) => throw new InvalidOperationException("Unexpected storage call."));

        await host.VerifyAsync(ApiService.Instance, "POST", "/api/Instance/" + action,
            new { CustomerGuid = hasCustomer ? Tenant : Guid.Empty, EdgeServerId = edgeServerId, Host = hostName, Port = port },
            reply => Assert.NotEmpty(reply.Json(status).Field("ErrorMessage").GetString()),
            hasCredential ? LauncherHeaders() : null);
    }
}
