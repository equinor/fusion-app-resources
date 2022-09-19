using Fusion.Resources.Functions.Integration.Http.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Fusion.Resources.Functions.Integration.Http
{
    public class HttpClientFactoryBuilder
    {
        private readonly IServiceCollection services;

        // Handle both exceptions and return values in one policy
        private HttpStatusCode[] httpStatusCodesWorthRetrying = {
           HttpStatusCode.FailedDependency, // This is worth retrying, as the dependency could recover...
           HttpStatusCode.RequestTimeout, // 408
           HttpStatusCode.BadGateway, // 502
           HttpStatusCode.ServiceUnavailable, // 503
           HttpStatusCode.GatewayTimeout // 504

           //HttpStatusCode.InternalServerError, // 500 - Chosing not retry on these, as they will most likely be programming errors, not connection issues.
        };

        internal HttpClientFactoryBuilder(IServiceCollection services)
        {
            this.services = services;
        }

  
        public HttpClientFactoryBuilder AddResourcesClient()
        {
            services.AddTransient<ResourcesHttpHandler>();
            services.AddHttpClient(HttpClientNames.Application.Resources, client =>
            {
                client.BaseAddress = new Uri("https://fusion-app-resources");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<ResourcesHttpHandler>()
            .AddTransientHttpErrorPolicy(DefaultRetryPolicy());

            return this;
        }

       
        private readonly TimeSpan[] DefaultSleepDurations = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) };

        private Func<PolicyBuilder<HttpResponseMessage>, IAsyncPolicy<HttpResponseMessage>> DefaultRetryPolicy(TimeSpan[] sleepDurations = null) =>
            policyBuilder =>
                 policyBuilder.OrResult(m => httpStatusCodesWorthRetrying.Contains(m.StatusCode))
                .WaitAndRetryAsync(sleepDurations ?? DefaultSleepDurations);

    }

}
