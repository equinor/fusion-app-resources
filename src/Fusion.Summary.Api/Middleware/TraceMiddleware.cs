using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Middleware
{
    public class TraceMiddleware
    {
        private readonly RequestDelegate next;

        public TraceMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;
            // Add a callback to when the request headers are about to be sent to the client. 
            // If the response is 
            context.Response.OnStarting(state => {
                var httpContext = (HttpContext)state;
                if (httpContext.Response.StatusCode >= 300)
                {
                    httpContext.Response.Headers.TryAdd("x-trace-id", traceId);
                }
                return Task.CompletedTask;
            }, context);
            // ....
            await next.Invoke(context);
        }
    }
}
