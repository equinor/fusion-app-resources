using Fusion.Resources.Functions;
using Fusion.Resources.Functions.Integration.Authentication;
using Fusion.Resources.Functions.Integration.Http;
using Fusion.Resources.Functions.Integration.ServiceDiscovery;
using Microsoft.Extensions.Configuration;
using System;
using Fusion.Resources.Functions.ApiClients;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddConfigServiceResolver(this IServiceCollection services)
        {
            services.AddSingleton<IServiceDiscovery, ConfigServiceResolver>();

            return services;
        }

        public static IServiceCollection AddAuthentication(this IServiceCollection services, Action<IConfiguration, AuthOptions> configure)
        {
            var config = services.BuildServiceProvider()
                .GetRequiredService<IConfiguration>();

            services.AddSingleton<ITokenProvider, FunctionTokenProvider>();
            services.Configure<AuthOptions>(opts => configure(config, opts));

            return services;
        }

        public static IServiceCollection AddHttpClients(this IServiceCollection services)
        {
            var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            services.Configure<HttpClientsOptions>(opt =>
            {
                opt.Fusion = config.GetValue<string>("Endpoints_Resources_Fusion");
            });

            var builder = new HttpClientFactoryBuilder(services);
            builder.AddOrgClient();
            services.AddOrgApiClient(HttpClientNames.Application.Org);

            builder.AddPeopleClient();
            services.AddScoped<IPeopleApiClient, PeopleApiClient>();
            
            builder.AddResourcesClient();
            services.AddScoped<IResourcesApiClient, ResourcesApiClient>();

            builder.AddLineOrgClient();
            services.AddScoped<ILineOrgApiClient, LineOrgApiClient>();

            return services;
        }
    }
}
