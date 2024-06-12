using Fusion.Summary.Functions.Integration.Authentication;
using Fusion.Summary.Functions.Integration.ServiceDiscovery;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Fusion.Summary.Functions.Startup))]

namespace Fusion.Summary.Functions
{
    class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddAuthentication((cfg, opts) =>
            {
                opts.ClientId = cfg.GetValue<string>("AzureAd_ClientId");
                opts.Secret = cfg.GetValue<string>("AzureAd_Secret");
                opts.TenantId = cfg.GetValue<string>("AzureAd_TenantId");
            });

            builder.Services.AddConfigServiceResolver();
            //builder.Services.AddHttpClients();
        }
    }

    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddAuthentication(this IServiceCollection services, Action<IConfiguration, AuthOptions> configure)
        {
            var config = services.BuildServiceProvider()
                .GetRequiredService<IConfiguration>();

            services.AddSingleton<ITokenProvider, FunctionTokenProvider>();
            services.Configure<AuthOptions>(opts => configure(config, opts));

            return services;
        }

        public static IServiceCollection AddConfigServiceResolver(this IServiceCollection services)
        {
            services.AddSingleton<IServiceDiscovery, ConfigServiceResolver>();

            return services;
        }
    }
}