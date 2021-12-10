using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fusion.Resources.Functions
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(builder =>
                {
                    builder.Services.AddAuthentication((cfg, opts) =>
                    {
                        opts.ClientId = cfg.GetValue<string>("AzureAd_ClientId");
                        opts.Secret = cfg.GetValue<string>("AzureAd_Secret");
                        opts.TenantId = cfg.GetValue<string>("AzureAd_TenantId");
                    });

                    builder.Services.AddNotificationServices();

                    builder.Services.AddServiceResolver();
                    builder.Services.AddHttpClients();
                    builder.Services.AddOrgApiClient(HttpClientNames.Application.Org);

                })

                .Build();

            await host.RunAsync();
        }
    }
}