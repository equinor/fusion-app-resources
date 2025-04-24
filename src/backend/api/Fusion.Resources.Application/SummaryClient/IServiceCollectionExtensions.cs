using System;
using Fusion.Integration.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fusion.Resources.Application.SummaryClient;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSummaryHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFusionIntegrationHttpClient(HttpClientNames.Summary, client =>
        {
            client.AllowNoBaseUrl = true;
            client.HttpClientConfig = (httpClient) => httpClient.BaseAddress = new Uri(configuration["InternalServiceDiscovery:Summary:Url"]
                                                                                       ?? throw new ArgumentException("Endpoint for Summary could not be resolved during startup"));

            // Does not work due to some package weirdness. At build time client.Resource exist, but at runtime it does not. Instead, it becomes client.Scope,
            // Maybe after updating all packages, or merging projects, or consolidating packages this will work.
            // So for now we assign base address at startup and get auth token manually in the ISummaryClient service.
            // client.Uri = new Uri(configuration["..."]!);
            // client.Resource = configuration["AzureAd:ClientId"];
        });

        services.AddScoped<ISummaryClient, SummaryClient>();

        return services;
    }
}