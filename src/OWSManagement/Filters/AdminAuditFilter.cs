using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using OWSData.Models.Composites;
using OWSData.Repositories.Interfaces;
using OWSShared.Interfaces;
using SimpleInjector;

namespace OWSManagement.Filters
{
    /// <summary>
    /// Writes an AuditLog row for every admin console change (any non-GET API action): the route, the arguments with
    /// passwords removed, the outcome and the caller's IP. The console has no login, so the actor is the console itself;
    /// through the SSH tunnel the IP is the Docker gateway.
    /// </summary>
    public class AdminAuditFilter : IAsyncActionFilter
    {
        private readonly Container _container;

        public AdminAuditFilter(Container container)
        {
            _container = container;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ActionExecutedContext executed = await next();

            HttpRequest request = context.HttpContext.Request;
            if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
            {
                return;
            }

            string outcome = executed.Exception != null && !executed.ExceptionHandled ? "error: " + executed.Exception.GetType().Name
                : (executed.Result as ObjectResult)?.Value is SuccessAndErrorMessage message ? (message.Success ? "ok" : "refused: " + message.ErrorMessage)
                : executed.Result is IStatusCodeActionResult status && status.StatusCode >= 400 ? "http " + status.StatusCode
                : "ok";

            var detail = new JsonObject
            {
                ["arguments"] = WithoutPasswords(JsonSerializer.SerializeToNode(context.ActionArguments)),
                ["outcome"] = outcome
            };

            await _container.GetInstance<IAuditRepository>().Add(
                _container.GetInstance<IHeaderCustomerGUID>().CustomerGUID,
                "management",
                "admin-console",
                "admin_change",
                $"{request.Method} {request.Path}",
                detail.ToJsonString(),
                context.HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private static JsonNode WithoutPasswords(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                foreach (string key in obj.Select(pair => pair.Key).ToList())
                {
                    if (key.Contains("password", StringComparison.OrdinalIgnoreCase))
                    {
                        obj.Remove(key);
                    }
                    else
                    {
                        WithoutPasswords(obj[key]);
                    }
                }
            }
            else if (node is JsonArray array)
            {
                foreach (JsonNode item in array)
                {
                    WithoutPasswords(item);
                }
            }
            return node;
        }
    }
}
