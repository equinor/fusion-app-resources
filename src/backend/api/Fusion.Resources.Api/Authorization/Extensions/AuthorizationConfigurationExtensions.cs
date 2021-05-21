using Fusion.Resources.Api.Authorization.Handlers;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AuthorizationConfigurationExtensions
    {
        public static IServiceCollection AddResourcesAuthorizationHandlers(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, ContractRoleAuthHandler>();
            services.AddScoped<IAuthorizationHandler, DelegatedContractRoleAuthHandler>();
            services.AddScoped<IAuthorizationHandler, ProjectAccessAuthHandler>();
            services.AddScoped<IAuthorizationHandler, RequestAccessAuthHandler>();
            services.AddScoped<IAuthorizationHandler, ContractorInContractHandler>();
            services.AddScoped<IAuthorizationHandler, ContractorInProjectHandler>();
            services.AddScoped<IAuthorizationHandler, OrgPositionAccessHandler>();
            services.AddScoped<IAuthorizationHandler, OrgProjectAccessHandler>();
            services.AddScoped<IAuthorizationHandler, RequestCreatorHandler>();

            return services;
        }
    }
}
