using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Fusion.Resources.Domain.Services.OrgClient;

public class OrgApiClient
{
    public OrgApiClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    public HttpClient HttpClient { get; }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        if (RequestHeadersScope.Current.Value != null)
        {
            foreach (var header in RequestHeadersScope.Current.Value.Headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        return HttpClient.SendAsync(request, cancellationToken);
    }

    public async Task<RequestResponse<T>> PostAsync<T>(string url, T content, CancellationToken cancellationToken = default)
    {
        var requestContent = JsonConvert.SerializeObject(content);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestContent, System.Text.Encoding.UTF8, "application/json")
        };

        var response = await SendAsync(request, cancellationToken);
        return await RequestResponse<T>.FromResponseAsync(response);
    }

    public RequestHeadersScope UseRequestHeaders()
    {
        return new RequestHeadersScope();
    }
}