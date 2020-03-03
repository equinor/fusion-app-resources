using System;
using System.Collections.Generic;
using System.Text;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Services;
using MediatR;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DomainConfigExtensions
    {

        public static IServiceCollection AddResourceDomain(this IServiceCollection services)
        {
            services.AddMediatR(typeof(DomainConfigExtensions));
            services.AddScoped<IProfileServices, ProfileServices>();

            return services;
        }
    }
}
