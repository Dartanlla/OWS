using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;

namespace OWSPublicAPI.Services
{
    public sealed class PublicApiTenant : IMiddleware
    {
        private readonly IHeaderCustomerGUID _tenant;
        private readonly IUsersRepository _users;

        public PublicApiTenant(IHeaderCustomerGUID tenant, IUsersRepository users)
        {
            _tenant = tenant;
            _users = users;
        }

        public static bool IsTenantScoped(string controller, string action) =>
            string.Equals(controller, "Characters", StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(controller, "Users", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(action, "RedeemExternalConnectionTicket", StringComparison.OrdinalIgnoreCase));

        public static async Task<Guid> ResolveAsync(string header, IUsersRepository users) =>
            Guid.TryParse(header, out var tenant) && tenant != Guid.Empty && await users.CustomerExists(tenant)
                ? tenant : Guid.Empty;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (action != null && IsTenantScoped(action.ControllerName, action.ActionName))
            {
                _tenant.CustomerGUID = await ResolveAsync(context.Request.Headers["X-CustomerGUID"].ToString(), _users);
                if (_tenant.CustomerGUID == Guid.Empty)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            await next(context);
        }
    }
}
