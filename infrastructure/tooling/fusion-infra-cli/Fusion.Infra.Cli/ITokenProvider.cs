// See https://aka.ms/new-console-template for more information
using Azure.Core;
using Fusion.Infra.Cli;
using Newtonsoft.Json;
using System.Net.Http.Json;
/// <summary>
/// Get token using microsoft authentication. For now this is using default credentials.
/// </summary>
public interface ITokenProvider
{
    Task<AccessToken> GetAccessToken(string resource);
}

public interface IAccountResolver
{
    Task<Guid?> ResolveAccountAsync(string identifier, bool returnNullOnAmbigiousMatch);
    Task<Guid?> ResolveAppRegServicePrincipalAsync(string identifier);
}

public class AccountResolver : IAccountResolver
{
    private HttpClient client;

    public AccountResolver(IHttpClientFactory httpClientFactory)
    {
        client = httpClientFactory.CreateClient(Constants.GraphClientName);
    }

    public async Task<Guid?> ResolveAccountAsync(string identifier, bool returnNullOnAmbigiousMatch)
    {
        var resp = await client.GetAsync($"/v1.0/servicePrincipals?$filter=displayName eq '{identifier}'");
        var content = await resp.Content.ReadAsStringAsync();

        resp.EnsureSuccessStatusCode();

        var results = JsonConvert.DeserializeAnonymousType(content, new { value = new[] { new { id = Guid.Empty } } });

        if (results?.value.Length == 1)
            return results.value.First().id;
        else if (results?.value.Length > 1)
        {
            Console.WriteLine($"# WARN - Located multiple service principals using the name '{identifier}': {string.Join(", ", results.value.Select(i => $"{i}"))}");

            if (returnNullOnAmbigiousMatch == true)
                return null;

            throw new InvalidOperationException($"Located multiple service principals using the name {identifier}");
        }

        return null;
    }

    public async Task<Guid?> ResolveAppRegServicePrincipalAsync(string identifier)
    {
        var resp = await client.GetAsync($"/v1.0/servicePrincipals(appId='{identifier}'");

        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        resp.EnsureSuccessStatusCode();

        var content = await resp.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeAnonymousType(content, new {id = Guid.Empty});
        return data?.id;

    }
}
