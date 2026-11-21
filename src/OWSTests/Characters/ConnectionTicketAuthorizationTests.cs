using OWSShared.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using OWSData.Models.Tickets;
using OWSData.Models.StoredProcs;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.DTOs;
using OWSPublicAPI.Services;
using Xunit;

namespace OWSTests.Characters
{
    public class ConnectionTicketAuthorizationTests
    {
        [Fact]
        public async Task IssuanceRequiresExactReadyAssignmentFromInstanceManagement()
        {
            var tenant = Guid.NewGuid();
            var session = Guid.NewGuid();
            var owner = Guid.NewGuid();
            const string variable = "OWS_INSTANCE_ASSIGNMENT_CREDENTIAL";
            var previous = Environment.GetEnvironmentVariable(variable);
            Environment.SetEnvironmentVariable(variable, new string('a', 32));
            try
            {
                bool ready = false;
                int stored = 0;
                var users = RepositoryProxy.Create<IUsersRepository>((method, args) =>
                    Task.FromResult(new GetUserSession
                    {
                        CustomerGuid = tenant, UserGuid = owner, UserSessionGUID = session,
                        CharacterID = 12, SelectedCharacterName = "Player", CharName = "Player"
                    }));
                var clients = new AssignmentClient(async message =>
                {
                    Assert.Equal("/api/Instance/CheckReadyAssignment", message.RequestUri.AbsolutePath);
                    Assert.Equal(new string('a', 32), Assert.Single(message.Headers.GetValues(ServerCredentials.AssignmentHeader)));
                    using var body = JsonDocument.Parse(await message.Content.ReadAsStringAsync());
                    Assert.Equal(tenant, body.RootElement.GetProperty("customerGuid").GetGuid());
                    Assert.Equal(12, body.RootElement.GetProperty("characterId").GetInt32());
                    Assert.Equal(owner, body.RootElement.GetProperty("userGuid").GetGuid());
                    int instanceId = body.RootElement.GetProperty("mapInstanceId").GetInt32();
                    int edgeId = body.RootElement.GetProperty("edgeServerId").GetInt32();
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(ready && instanceId == 13 && edgeId == 7 ? "true" : "false")
                    };
                });
                var tickets = RepositoryProxy.Create<ITicketRepository>((method, args) =>
                {
                    Assert.Equal(nameof(ITicketRepository.AddTicketAsync), method.Name);
                    var ticket = Assert.IsType<ConnectionTicket>(args[0]);
                    Assert.Equal(7, ticket.ServerId);
                    Assert.Equal(13, ticket.MapInstanceId);
                    Assert.Equal(tenant, ticket.CustomerGuid);
                    Assert.InRange((TimeSpan)args[1], TimeSpan.FromSeconds(40), TimeSpan.FromSeconds(45));
                    stored++;
                    return Task.FromResult(true);
                });
                var issuer = new ExternalConnectionTicketIssuer(users, clients, tickets);
                var request = new ExternalConnectionTicketRequest { UserSessionGUID = session, ServerId = 8, MapInstanceId = 13 };
                Assert.Equal(TicketIssueStatus.IneligibleServer, (await issuer.IssueAsync(tenant, request)).Status);
                request.ServerId = 7;
                Assert.Equal(TicketIssueStatus.IneligibleServer, (await issuer.IssueAsync(tenant, request)).Status);
                Assert.Equal(0, stored);

                ready = true;
                Assert.Equal(TicketIssueStatus.Issued, (await issuer.IssueAsync(tenant, request)).Status);
                Assert.Equal(1, stored);
                request.MapInstanceId = 14;
                Assert.Equal(TicketIssueStatus.IneligibleServer, (await issuer.IssueAsync(tenant, request)).Status);
                request.MapInstanceId = 13;
                request.ServerId = 8;
                Assert.Equal(TicketIssueStatus.IneligibleServer, (await issuer.IssueAsync(tenant, request)).Status);
            }
            finally
            {
                Environment.SetEnvironmentVariable(variable, previous);
            }
        }
        private sealed class AssignmentClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
            : HttpMessageHandler, IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(this, disposeHandler: false)
            {
                BaseAddress = new Uri("https://instance-management.example/")
            };

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => send(request);
        }
    }
}
