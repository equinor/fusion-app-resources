using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;


[assembly: FunctionsStartup(typeof(Fusion.Resources.Functions.Startup))]

namespace Fusion.Resources.Functions
{
    class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddAuthentication((cfg, opts) => {
                opts.ClientId = cfg.GetValue<string>("AzureAd_ClientId");
                opts.Secret = cfg.GetValue<string>("AzureAd_Secret");
                opts.TenantId = cfg.GetValue<string>("AzureAd_TenantId");
            });

            builder.Services.AddServiceResolver();
            builder.Services.AddHttpClients();
        }
    }

}