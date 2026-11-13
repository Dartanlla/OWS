using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using OWSData.Repositories.Interfaces;
using OWSPublicAPI.Services;
using OWSShared.Implementations;
using Xunit;

namespace OWSTests.Characters
{
    public class PublicApiTenantTests
    {
        [Theory]
        [InlineData("/api/System/Status", "missing", true, 0)]
        [InlineData("/api/Users/RedeemExternalConnectionTicket", "missing", true, 0)]
        [InlineData("/api/Users/LoginAndCreateSession", "missing", false, 0)]
        [InlineData("/api/Users/RegisterUser", "malformed", false, 0)]
        [InlineData("/api/Characters/42", "unknown", false, 1)]
        [InlineData("/API/CHARACTERS/42/", "known", true, 1)]
        public async Task OnlyTenantScopedRoutesRequireKnownTenant(string path, string header, bool allowed, int lookups)
        {
            var knownTenant = Guid.NewGuid();
            int calls = 0;
            var users = RepositoryProxy.Create<IUsersRepository>((method, args) =>
            {
                Assert.Equal(nameof(IUsersRepository.CustomerExists), method.Name);
                calls++;
                return Task.FromResult((Guid)args[0] == knownTenant);
            });
            var context = new DefaultHttpContext();
            var segments = path.Trim('/').Split('/');
            context.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new ControllerActionDescriptor
            {
                ControllerName = segments[1],
                ActionName = segments[2]
            }), path));
            if (header != "missing")
                context.Request.Headers["X-CustomerGUID"] = header switch
                {
                    "known" => knownTenant.ToString(),
                    "unknown" => Guid.NewGuid().ToString(),
                    _ => "invalid"
                };
            var tenant = new HeaderCustomerGUID();
            bool invoked = false;

            await new PublicApiTenant(tenant, users).InvokeAsync(context, _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            Assert.Equal(allowed, invoked);
            Assert.Equal(allowed ? 200 : 401, context.Response.StatusCode);
            Assert.Equal(header == "known" ? knownTenant : Guid.Empty, tenant.CustomerGUID);
            Assert.Equal(lookups, calls);
        }
    }
}
