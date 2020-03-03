using System;
using System.Collections.Generic;
using System.Text;
using MediatR;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DomainConfigExtensions
    {

        public static IServiceCollection AddResourceDomain(this IServiceCollection services)
        {
            services.AddMediatR(typeof(DomainConfigExtensions));

            return services;
        }
    }
}
