using System.Net;
using System.Net.Http.Headers;
using Fusion.Resources.Functions.Common.Integration.Http.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Fusion.Resources.Functions.Common.Integration.Http
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

        public HttpClientFactoryBuilder AddPeopleClient()
        {
            services.AddTransient<PeopleHttpHandler>();
            services.AddHttpClient(HttpClientNames.Application.People, client =>
                {
                    client.BaseAddress = new Uri("https://fusion-people");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                })
                .AddHttpMessageHandler<PeopleHttpHandler>()
                .AddTransientHttpErrorPolicy(DefaultRetryPolicy());

            return this;
        }
        public HttpClientFactoryBuilder AddOrgClient()
        {
            services.AddTransient<OrgHttpHandler>();
            services.AddHttpClient(HttpClientNames.Application.Org, client =>
                {
                    client.BaseAddress = new Uri("https://fusion-org");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                })
                .AddHttpMessageHandler<OrgHttpHandler>()
                .AddTransientHttpErrorPolicy(DefaultRetryPolicy());

            return this;
        }

        public HttpClientFactoryBuilder AddNotificationsClient()
        {
            services.AddTransient<NotificationsHttpHandler>();
            services.AddHttpClient(HttpClientNames.Application.Notifications, client =>
            {
                client.BaseAddress = new Uri("https://fusion-app-notifications");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<NotificationsHttpHandler>()
            .AddTransientHttpErrorPolicy(DefaultRetryPolicy());

            return this;
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

        public HttpClientFactoryBuilder AddSummaryClient()
        {
            services.AddTransient<SummaryHttpHandler>();
            // TODO: Should summary have its own application registration?
            services.AddHttpClient(HttpClientNames.Application.Resources, client =>
                {
                    client.BaseAddress = new Uri("https://fusion-org");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                })
                .AddHttpMessageHandler<SummaryHttpHandler>()
                .AddTransientHttpErrorPolicy(DefaultRetryPolicy());

            return this;
        }

        public HttpClientFactoryBuilder AddLineOrgClient()
        {
            services.AddTransient<LineOrgHttpHandler>();
            services.AddHttpClient(HttpClientNames.Application.LineOrg, client =>
                {
                    client.BaseAddress = new Uri("https://fusion-notifications");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                })
                .AddHttpMessageHandler<LineOrgHttpHandler>()
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
