using Fusion.Resources.Functions.Common.Configuration;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

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
            builder.Services.AddHttpClients();
        }
    }
}