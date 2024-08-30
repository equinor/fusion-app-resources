using Fusion.Resources.Functions.Common.Configuration;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Fusion.Resources.Functions.Startup))]

namespace Fusion.Resources.Functions
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
//
            builder.Services.AddConfigServiceResolver();
            builder.Services.AddHttpClients();
        }
    }
}