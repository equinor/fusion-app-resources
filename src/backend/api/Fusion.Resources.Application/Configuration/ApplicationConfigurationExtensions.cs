using Fusion.Resources;
using Fusion.Resources.Application.People;
using Fusion.Resources.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationConfigurationExtensions
    {
        public static IServiceCollection AddResourcesApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IQueueSender, ServiceBusQueueSender>();
            services.AddScoped<IPeopleIntegration, PeopleIntegration>();
            return services;
        }
    }
}
