using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OWSData.Models.Tables;
using OWSData.Models.Tickets;
using OWSData.Repositories.Interfaces;
using OWSShared.Options;
using Xunit;
using static OWSTests.Api.ApiTestHost;

namespace OWSTests.Api;

[Collection(ApiTestCollection.Name)]
public class ConnectionApiTests
{
    private const string IssuePath = "/api/Users/ExternalConnectionTicket";
    private const string RedeemPath = "/api/Instance/RedeemExternalConnectionTicket";
    private const string LauncherCredential = "example-regression-launcher-credential";
    private const string AssignmentCredential = "example-regression-assignment-credential";
    private const string ServerCredential = "example-regression-instance-credential";
    private const string TicketValue = "example-ticket";
    private const string Claimant = "example-claimant";
    private const int Destination = 29;
    private static readonly string ServerHash = ServerCredentials.Hash(ServerCredential);

    [Fact]
    public async Task ZoneStatusRejectsTheFormerEdgeReadinessPayload()
    {
        const string path = "/api/Instance/SetZoneInstanceStatus";
        var host = new ApiTestHost();
        host.Add<IInstanceManagementRepository>((_, _) => throw new InvalidOperationException("Unexpected map status update."));
        await host.VerifyAsync(ApiService.Instance, "POST", path, new { CustomerGuid = Tenant, MapInstanceId = Destination },
            reply => reply.Is(400), Headers(ServerCredentials.CredentialHeader, ServerCredential));
    }

    [Theory]
    [InlineData("CheckReadyAssignment", "assignment", 200)]
    [InlineData("CheckReadyAssignment", "assignment", 400)]
    [InlineData("CheckReadyAssignment", "launcher", 401)]
    [InlineData("GetReadyMapInstances", "launcher", 200)]
    [InlineData("GetReadyMapInstances", "none", 401)]
    [InlineData("AddMapInstance", "launcher", 201)]
    [InlineData("AddMapInstance", "launcher", 400)]
    [InlineData("AddMapInstance", "launcher", 404)]
    [InlineData("AddMapInstance", "assignment", 401)]
    public async Task InstanceOperationsValidateTheirInputsAndUseTheRequiredCredential(string action, string credential, int status)
    {
        using var launcher = new EnvironmentSetting("OWS_INSTANCE_LAUNCHER_CREDENTIAL", LauncherCredential);
        using var assignment = new EnvironmentSetting("OWS_INSTANCE_ASSIGNMENT_CREDENTIAL", AssignmentCredential);
        var headers = credential switch
        {
            "launcher" => Headers(ServerCredentials.LauncherHeader, LauncherCredential),
            "assignment" => Headers(ServerCredentials.AssignmentHeader, AssignmentCredential),
            _ => null
        };
        var body = new MapInstanceOperation
        {
            CustomerGuid = Tenant, MapInstanceId = 17, WorldServerId = 3, MapId = 9, Port = 7777,
            CharacterId = 42, UserGuid = Owner, EdgeServerId = 19
        };
        if (status == 400)
        {
            if (action == "AddMapInstance") body.Port = 65536;
            if (action == "CheckReadyAssignment") body.CharacterId = 0;
        }

        var host = new ApiTestHost();
        int repositoryCalls = 0;
        host.Add<IInstanceManagementRepository>((method, args) =>
        {
            repositoryCalls++;
            switch (action)
            {
                case "CheckReadyAssignment":
                    Assert.Equal(nameof(IInstanceManagementRepository.HasReadyAssignment), method.Name);
                    Assert.Equal(new object[] { Tenant, 42, Owner, 17, 19 }, args);
                    return Task.FromResult(true);
                case "GetReadyMapInstances":
                    Assert.Equal(action, method.Name);
                    Assert.Equal(Tenant, Assert.Single(args));
                    return Task.FromResult<IEnumerable<MapInstances>>([new() { MapInstanceId = 17, Status = 2 }]);
                case "AddMapInstance":
                    Assert.Equal(action, method.Name);
                    Assert.Equal(new object[] { Tenant, 3, 9, 7777 }, args);
                    return Task.FromResult(status == 201 ? 17 : 0);
                default:
                    throw new InvalidOperationException(action);
            }
        });

        await host.VerifyAsync(ApiService.Instance, "POST", "/api/Instance/" + action, body, reply =>
        {
            var json = reply.Json(status);
            if (status >= 400)
                Assert.NotEmpty(json.Field("ErrorMessage").GetString());
            else if (action == "CheckReadyAssignment")
                Assert.True(json.GetBoolean());
            else if (action == "GetReadyMapInstances")
                Assert.Equal(17, Assert.Single(json.EnumerateArray()).Field("MapInstanceId").GetInt32());
            else if (action == "AddMapInstance")
                Assert.Equal(17, json.Field("MapInstanceId").GetInt32());
            else
                Assert.True(json.Field("Success").GetBoolean());

            var expectedCalls = status is 400 or 401 ? 0 : 1;
            Assert.Equal(expectedCalls, repositoryCalls);
            repositoryCalls = 0;
        }, headers);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(503)]
    public async Task TicketIssuanceRequiresASelectedCharacterAndReadyAssignment(int status)
    {
        using var credential = new EnvironmentSetting("OWS_INSTANCE_ASSIGNMENT_CREDENTIAL", AssignmentCredential);
        var host = PublicApiTests.PlayerHost(session: () => PublicApiTests.PlayerSession(status == 401 ? null : "Aster"));
        int assignmentChecks = 0;
        int writes = 0;
        ConnectionTicket written = null;
        using var instanceClient = new InstanceClient(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/Instance/CheckReadyAssignment", request.RequestUri.AbsolutePath);
            Assert.Equal(AssignmentCredential, Assert.Single(request.Headers.GetValues(ServerCredentials.AssignmentHeader)));
            using var json = JsonDocument.Parse(await request.Content.ReadAsStringAsync());
            Assert.Equal(Tenant, json.RootElement.Field("CustomerGuid").GetGuid());
            Assert.Equal(Owner, json.RootElement.Field("UserGuid").GetGuid());
            Assert.Equal(42, json.RootElement.Field("CharacterId").GetInt32());
            Assert.Equal(Destination, json.RootElement.Field("MapInstanceId").GetInt32());
            Assert.Equal(17, json.RootElement.Field("EdgeServerId").GetInt32());
            assignmentChecks++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(status == 403 ? "false" : "true") };
        });
        host.Add<IHttpClientFactory>(instanceClient);
        host.Add<ITicketRepository>((method, args) =>
        {
            Assert.Equal(nameof(ITicketRepository.AddTicketAsync), method.Name);
            written = Assert.IsType<ConnectionTicket>(args[0]);
            Assert.Equal(Tenant, written.CustomerGuid);
            Assert.Equal(Owner, written.UserGuid);
            Assert.Equal(Session, written.UserSessionGuid);
            Assert.Equal(42, written.CharacterId);
            Assert.Equal(17, written.ServerId);
            Assert.Equal(Destination, written.MapInstanceId);
            Assert.InRange(Assert.IsType<TimeSpan>(args[1]), TimeSpan.Zero, TimeSpan.FromSeconds(45));
            writes++;
            return Task.FromResult(status != 503);
        });

        await host.VerifyAsync(ApiService.Public, "POST", IssuePath,
            new { UserSessionGUID = Session, ServerId = 17, MapInstanceId = status == 400 ? 0 : Destination }, reply =>
        {
            reply.Is(status);
            Assert.Equal("no-store", reply.CacheControl);
            Assert.Equal(status is 400 or 401 ? 0 : 1, assignmentChecks);
            Assert.Equal(status == 200 ? 1 : status == 503 ? 3 : 0, writes);
            if (status == 200)
            {
                var json = reply.Json();
                Assert.Equal(written.Ticket, json.Field("Ticket").GetString());
                Assert.Equal(Destination, json.Field("MapInstanceId").GetInt32());
                Assert.Equal(written.SessionSecretBase64, json.Field("SessionSecretBase64").GetString());
                Assert.Equal(32, Convert.FromBase64String(written.SessionSecretBase64).Length);
                Assert.Equal(written.ExpiresAtUtc, json.Field("ExpiresAtUtc").GetDateTimeOffset());
            }
            assignmentChecks = writes = 0;
        });
    }

    [Fact]
    public async Task PublicTicketIssuerRequiresHttpsAndAnAvailableTicketStore()
    {
        var host = new ApiTestHost();
        using var server = host.Start(ApiService.Public);
        var request = new { UserSessionGUID = Session, ServerId = 17, MapInstanceId = Destination };
        var insecure = await SendAsync(server, "POST", IssuePath, request, useHttps: false);
        insecure.Is(403);
        Assert.Equal("no-store", insecure.CacheControl);
        (await SendAsync(server, "POST", IssuePath, request)).Is(503);
        (await host.SendLambdaAsync(ApiService.Public, "POST", IssuePath, request)).Is(503);
    }

    [Theory]
    [InlineData(false, true, 200)]
    [InlineData(true, true, 200)]
    [InlineData(true, true, 400)]
    [InlineData(false, true, 401)]
    [InlineData(false, false, 404)]
    [InlineData(true, true, 404)]
    [InlineData(true, true, 503)]
    public async Task RedemptionChecksPrivateTransportAndForwardsInspectOrClaim(bool claim, bool https, int status)
    {
        using var port = new EnvironmentSetting("OWS_EDGE_PRIVATE_HTTPS_PORT", PrivatePort.ToString());
        var host = new ApiTestHost();
        int authentications = 0;
        int lookups = 0;
        host.Add<IInstanceManagementRepository>((method, args) =>
        {
            Assert.Equal(nameof(IInstanceManagementRepository.AuthenticateActiveEdgeServer), method.Name);
            Assert.Equal(new object[] { 17, ServerHash }, args);
            authentications++;
            return Task.FromResult<Guid?>(status == 401 ? null : Tenant);
        });
        host.Add<ITicketRepository>((method, args) =>
        {
            Assert.Equal(claim ? nameof(ITicketRepository.RedeemTicketAsync) : nameof(ITicketRepository.GetTicketAsync), method.Name);
            Assert.Equal(claim ? new object[] { TicketValue, Tenant, 17, Claimant } : [TicketValue, Tenant, 17], args);
            lookups++;
            if (status == 503)
                return Task.FromException<ConnectionTicket>(new InvalidOperationException("Ticket store unavailable"));
            return Task.FromResult(status == 404 ? null : new ConnectionTicket
            {
                Ticket = TicketValue, CustomerGuid = Tenant, UserGuid = Owner, UserSessionGuid = Session,
                CharacterId = 42, ServerId = 17, MapInstanceId = Destination, SessionSecretBase64 = "example-secret"
            });
        });

        var body = new { Ticket = TicketValue, ServerId = 17, Claim = claim, ClaimantId = status == 400 ? "" : Claimant };
        var headers = Headers(ServerCredentials.CredentialHeader, ServerCredential);
        using var server = host.Start(ApiService.Instance);
        var reply = await SendAsync(server, "POST", RedeemPath, body, headers: headers, useHttps: https);
        reply.Is(status);
        Assert.Equal("no-store", reply.CacheControl);
        Assert.Equal(https && status != 400 ? 1 : 0, authentications);
        Assert.Equal(https && status is 200 or 404 or 503 ? 1 : 0, lookups);
        if (status == 200)
        {
            var json = reply.Json();
            Assert.Equal(Tenant, json.Field("CustomerGuid").GetGuid());
            Assert.Equal(Session, json.Field("UserSessionGuid").GetGuid());
            Assert.Equal(42, json.Field("CharacterId").GetInt32());
            Assert.Equal(17, json.Field("ServerId").GetInt32());
            Assert.Equal(Destination, json.Field("MapInstanceId").GetInt32());
        }
    }

    [Theory]
    [InlineData("GET", null, false, 405)]
    [InlineData("POST", "{", true, 400)]
    [InlineData("POST", """{"Ticket":"example-ticket","ServerId":17,"Claim":true}""", false, 400)]
    [InlineData("POST", """{"Ticket":"example-ticket","ServerId":17}""", true, 401)]
    public async Task LambdaRedemptionValidatesTheGatewayRequest(string method, string body, bool base64, int status)
    {
        var host = new ApiTestHost();
        var reply = await host.SendLambdaAsync(ApiService.Instance, method, RedeemPath, body, base64: base64);
        Assert.NotEmpty(reply.Json(status).Field("ErrorMessage").GetString());
        Assert.Equal("no-store", reply.CacheControl);
    }

    [Fact]
    public async Task LambdaRedemptionReportsAnUnconfiguredStoreAfterAuthenticatingTheEdge()
    {
        using var connection = new EnvironmentSetting("OWSTicketConfig__ConnectionString", null);
        var host = new ApiTestHost();
        int authentications = 0;
        host.Add<IInstanceManagementRepository>((method, args) =>
        {
            Assert.Equal(nameof(IInstanceManagementRepository.AuthenticateActiveEdgeServer), method.Name);
            Assert.Equal(new object[] { 17, ServerHash }, args);
            authentications++;
            return Task.FromResult<Guid?>(Tenant);
        });
        var reply = await host.SendLambdaAsync(ApiService.Instance, "POST", RedeemPath,
            new { Ticket = TicketValue, ServerId = 17, Claim = true, ClaimantId = Claimant },
            headers: Headers(ServerCredentials.CredentialHeader, ServerCredential), base64: true);
        Assert.Contains("not configured", reply.Json(503).Field("ErrorMessage").GetString());
        Assert.Equal("no-store", reply.CacheControl);
        Assert.Equal(1, authentications);
    }

    private static Dictionary<string, string> Headers(string name, string value) => new() { [name] = value };
}
