using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Fusion.Resources.Infrastructure.Middleware;

public class SwaggerAuthMiddleware
{
    private readonly RequestDelegate next;

    public SwaggerAuthMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task Invoke(HttpContext httpContext, IWebHostEnvironment env)
    {
        if (IsSwaggerUiPath(httpContext))
        {
            var result = await httpContext.AuthenticateAsync(SwaggerAuthDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                await httpContext.ChallengeAsync(SwaggerAuthDefaults.AuthenticationScheme);
                return;
            }

            await next(httpContext);
        }
    }

    public static bool IsSwaggerUiPath(HttpContext httpContext)
    {
        var swaggerOptions = httpContext.RequestServices.GetRequiredService<IOptions<SwaggerUIOptions>>();
        var pathPrefix = swaggerOptions.Value.RoutePrefix;
        return httpContext.Request.Path.StartsWithSegments($"/{pathPrefix.TrimStart('/')}");
    }
}