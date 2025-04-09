using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.Integration.Authentication;
using Fusion.Resources.Functions.Common.Integration.Http;
using Fusion.Resources.Functions.Common.Integration.ServiceDiscovery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fusion.Resources.Functions.Common.Configuration
{
    public static class ServiceCollectionExtensions
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
            services.AddScoped<IOrgClient, OrgClient>();

            builder.AddSummaryClient();
            services.AddScoped<ISummaryApiClient, SummaryApiClient>();

            builder.AddPeopleClient();
            services.AddScoped<IPeopleApiClient, PeopleApiClient>();

            builder.AddResourcesClient();
            services.AddScoped<IResourcesApiClient, ResourcesApiClient>();

            builder.AddLineOrgClient();
            services.AddScoped<ILineOrgApiClient, LineOrgApiClient>();

            builder.AddNotificationsClient();
            services.AddScoped<INotificationApiClient, NotificationApiClient>();

            builder.AddOrgClient();
            services.AddScoped<IOrgClient, OrgClient>();

            builder.AddRolesClient();
            services.AddScoped<IRolesApiClient, RolesApiClient>();

            builder.AddMailClient();
            services.AddScoped<IMailApiClient, MailApiClient>();

            builder.AddContextClient();
            services.AddScoped<IContextApiClient, ContextApiClient>();

            return services;
        }
    }
}
