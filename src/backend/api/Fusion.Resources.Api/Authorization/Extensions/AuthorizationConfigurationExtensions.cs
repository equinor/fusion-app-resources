using Fusion.Resources.Api.Authorization.Handlers;
using Fusion.Resources.Authorization.Handlers;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AuthorizationConfigurationExtensions
    {
        public static IServiceCollection AddResourcesAuthorizationHandlers(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, OrgPositionAccessHandler>();
            services.AddScoped<IAuthorizationHandler, OrgProjectAccessHandler>();
            services.AddScoped<IAuthorizationHandler, RequestCreatorHandler>();
            services.AddScoped<IAuthorizationHandler, TaskOwnerForPositionAuthHandler>();

            return services;
        }
    }
}
