using Fusion.Resources.Api.Authorization.Handlers;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AuthorizationConfigurationExtensions
    {
        public static IServiceCollection AddResourcesAuthorizationHandlers(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, ContractRoleAuthHandler>();
            services.AddScoped<IAuthorizationHandler, ProjectAccessAuthHandler>();
            services.AddScoped<IAuthorizationHandler, RequestAccessAuthHandler>();
            services.AddScoped<IAuthorizationHandler, ContractorInContractHandler>();
            services.AddScoped<IAuthorizationHandler, ContractorInProjectHandler>();

            return services;
        }
    }
}
