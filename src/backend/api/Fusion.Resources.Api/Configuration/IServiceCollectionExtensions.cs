using Fusion.Integration.Configuration;
using Fusion.Integration.Http;
using Fusion.Resources.Api.Configuration;
using Fusion.Resources.Api.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HttpClientNames = Fusion.Resources.Api.Configuration.HttpClientNames;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddCommonLibHttpClient(this IServiceCollection services)
        {
            services.AddTransient<ApplicationCommonLibMessageHandler>();

            // Timeout for an individual try
            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(10);

            services.AddHttpClient(HttpClientNames.AppCommonLib, client =>
            {
                // This is just to allow relative urls on the http client 
                // - the actual endpoint is resolved by the handler
                client.BaseAddress = new Uri("https://not-configured.commonlib.fusion.equinor.com");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(90);
            })
            .AddHttpMessageHandler<ApplicationCommonLibMessageHandler>()
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(new[] {
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(60)
            }))
            .AddPolicyHandler(timeoutPolicy);

            return services;
        }

        public static IServiceCollection AddLineOrgHttpClient(this IServiceCollection services)
        {
            services.AddFusionIntegrationHttpClient("lineorg", o =>
            {
                o.EndpointResolver = (sp) =>
                {
                    var intgConfig = sp.GetRequiredService<IOptions<FusionIntegrationOptions>>();
                    var fusionEnv = intgConfig.Value.ServiceDiscovery?.Environment ?? "ci";
                    return Task.FromResult($"https://fusion-s-lineorg-{fusionEnv}.azurewebsites.net");
                };

                // Bug, must be specified
                o.Uri = new Uri("https://fusion-s-lineorg-.azurewebsits.net");

                //o.Resource 
            });

            return services;
        }
    }
}
