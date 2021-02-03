using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Fusion.Resources.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, configBuilder) =>
                {
                    if (ShouldLoadConfiguration())
                    {
                        configBuilder.AddJsonFile("/app/secrets/appsettings.secrets.yaml", optional: true);

                        AddKeyVault(ctx, configBuilder);

                        // Override key vault
                        if (ctx.HostingEnvironment.IsDevelopment())
                            configBuilder.AddUserSecrets<Program>();
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });


        private static void AddKeyVault(HostBuilderContext hostBuilderContext, IConfigurationBuilder configBuilder)
        {
            var tempConfig = configBuilder.Build();
            var clientId = tempConfig["AzureAd:ClientId"];
            var clientSecret = tempConfig["AzureAd:ClientSecret"];
            var keyVaultUrl = tempConfig["KEYVAULT_URL"];

            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                Console.WriteLine($"Adding key vault using url: '{keyVaultUrl}', client id '{clientId}' and client secret {(string.IsNullOrEmpty(clientSecret) ? "[empty]" : "*****")}");

                configBuilder.AddAzureKeyVault(keyVaultUrl, clientId, clientSecret);
            }
            else
            {
                Console.WriteLine("Skipping key vault as url is empty.");
            }
        }

        private static bool ShouldLoadConfiguration()
        {
            var integrationTestMarker = Environment.GetEnvironmentVariable("INTEGRATION_TEST_RUN");
            
            if (string.IsNullOrEmpty(integrationTestMarker))
                return true;

            if (string.Equals(integrationTestMarker, "true", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

    }
}
