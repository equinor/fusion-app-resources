using Fusion.Resources.Functions;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.Integration.Authentication;
using Fusion.Resources.Functions.Integration.Http;
using Fusion.Resources.Functions.Integration.ServiceDiscovery;
using Fusion.Resources.Functions.Notifications;
using Fusion.Resources.Functions.TableStorage;
using Microsoft.Extensions.Configuration;
using System;

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
            builder.AddPeopleClient();
            builder.AddResourcesClient();
            builder.AddNotificationsClient();
            builder.AddContextClient();
            builder.AddLineOrgClient();

            return services;
        }

        public static IServiceCollection AddRequiredResourcesFunctionsServices(this IServiceCollection services)
        {
            services.AddSingleton<TableStorageClient>();
            services.AddScoped<IResourcesApiClient, ResourcesApiClient>();
            services.AddScoped<INotificationApiClient, NotificationApiClient>();
            services.AddScoped<ISentNotificationsTableClient, SentNotificationsTableClient>();
            services.AddScoped<IContextApiClient, ContextApiClient>();
            services.AddScoped<ILineOrgApiClient, LineOrgApiClient>();
            services.AddScoped<IPeopleApiClient, PeopleApiClient>();
            services.AddScoped<IUrlResolver, UrlResolver>();
            services.AddScoped<RequestNotificationSender>();

            return services;
        }
    }
}
