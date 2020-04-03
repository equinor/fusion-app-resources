using System;
using System.Collections.Generic;
using System.Text;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Behaviours;
using Fusion.Resources.Domain.Services;
using MediatR;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DomainConfigExtensions
    {

        public static IServiceCollection AddResourceDomain(this IServiceCollection services)
        {
            services.AddMediatR(typeof(DomainConfigExtensions));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TrackableRequestBehaviour<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TelemetryBehaviour<,>));

            services.AddScoped<IProfileService, ProfileServices>();

            services.AddSingleton<ICompanyResolver, PeopleCompanyResolver>();

            return services;
        }
    }
}
