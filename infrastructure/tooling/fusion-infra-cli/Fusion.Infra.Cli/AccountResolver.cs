// See https://aka.ms/new-console-template for more information
using Fusion.Infra.Cli;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class AccountResolver : IAccountResolver
{
    private readonly ILogger<AccountResolver> logger;
    private HttpClient client;

    public AccountResolver(ILogger<AccountResolver> logger, IHttpClientFactory httpClientFactory)
    {
        client = httpClientFactory.CreateClient(Constants.GraphClientName);
        this.logger = logger;
    }

    public async Task<Guid?> ResolveAccountAsync(string identifier, bool returnNullOnAmbigiousMatch)
    {
        logger.BeginScope("Resolve account [{Identifier}]", identifier);

        var resp = await client.GetAsync($"/v1.0/servicePrincipals?$filter=displayName eq '{identifier}'");
        var content = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            logger.LogWarning($"{resp.RequestMessage?.Method} {resp.RequestMessage?.RequestUri} → {resp.StatusCode}");
            logger.LogWarning($"-- resp: {content}");
        }
        
        resp.EnsureSuccessStatusCode();

        var results = JsonConvert.DeserializeAnonymousType(content, new { value = new[] { new { id = Guid.Empty } } });

        if (results?.value.Length == 1)
            return results.value.First().id;
        else if (results?.value.Length > 1)
        {
            logger.LogWarning($"Located multiple service principals using the name '{identifier}': {string.Join(", ", results.value.Select(i => $"{i}"))}");

            if (returnNullOnAmbigiousMatch == true)
                return null;

            throw new InvalidOperationException($"Located multiple service principals using the name {identifier}");
        }

        return null;
    }

    public async Task<Guid?> ResolveAppRegServicePrincipalAsync(string identifier)
    {
        logger.BeginScope("Resolve app reg SP, client id: [{Identifier}]", identifier);

        var resp = await client.GetAsync($"/v1.0/servicePrincipals(appId='{identifier}')");

        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        var content = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            logger.LogError($"{resp.RequestMessage?.Method} {resp.RequestMessage?.RequestUri} → {resp.StatusCode}");
            logger.LogError($"-- resp: {content}");
        }

        resp.EnsureSuccessStatusCode();

        var data = JsonConvert.DeserializeAnonymousType(content, new {id = Guid.Empty});
        return data?.id;

    }
}
