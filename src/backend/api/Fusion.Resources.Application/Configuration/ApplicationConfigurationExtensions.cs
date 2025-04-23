using Fusion.Resources;
using Fusion.Resources.Application.LineOrg;
using Fusion.Resources.Application.People;
using Fusion.Resources.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Fusion.Resources.Application.Summary;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationConfigurationExtensions
    {
        public static IServiceCollection AddResourcesApplicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IQueueSender, ServiceBusQueueSender>();
            services.AddScoped<IPeopleIntegration, PeopleIntegration>();
            services.AddScoped<ILineOrgClient, LineOrgClient>();
            services.AddScoped<ISummaryClient, SummaryClient>();
            return services;
        }
    }
}
