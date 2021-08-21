using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OWSShared.Interfaces;

namespace OWSShared.Middleware
{
    public class StoreCustomerGUIDMiddleware : IMiddleware
    {
        private readonly IHeaderCustomerGUID _customerGuid;

        public StoreCustomerGUIDMiddleware(IHeaderCustomerGUID customerGuid)
        {
            _customerGuid = customerGuid;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                _customerGuid.CustomerGUID = Guid.Parse(context.Request.Headers.FirstOrDefault(x =>
                    string.Equals(x.Key, "X-CustomerGUID", StringComparison.CurrentCultureIgnoreCase)).Value.ToString());

                if (_customerGuid.CustomerGUID == Guid.Empty)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
            }
            catch (Exception)
            {
            }

            await next(context);
        }
    }
}
