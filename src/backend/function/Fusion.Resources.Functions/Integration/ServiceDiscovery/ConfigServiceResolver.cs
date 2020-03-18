using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Integration.ServiceDiscovery
{
    public class ConfigServiceResolver : IServiceDiscovery
    {
        private readonly IConfiguration configuration;

        public ConfigServiceResolver(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public Task<string> ResolveServiceAsync(ServiceEndpoint endpoint)
        {
            var configPath = $"Endpoints_{endpoint.Key}";
            var url = configuration.GetValue<string>(configPath);

            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException($"Missing endpoint config for key {endpoint.Key}");

            return Task.FromResult(url);
        }


    }
}
