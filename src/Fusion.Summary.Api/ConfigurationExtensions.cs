using Azure.Identity;

namespace Fusion.Summary.Api;

public static class ConfigurationExtensions
{
    public static void AddKeyVault(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var clientId = configuration["AzureAd:ClientId"];
        var tenantId = configuration["AzureAd:TenantId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];
        var keyVaultUrl = configuration["KEYVAULT_URL"];

        if (string.IsNullOrWhiteSpace(keyVaultUrl))
        {
            Console.WriteLine("Skipping key vault as url is empty or whitespace.");
            return;
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            Console.WriteLine("Skipping key vault as clientSecret is empty or whitespace.");
            return;
        }

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
    }
}