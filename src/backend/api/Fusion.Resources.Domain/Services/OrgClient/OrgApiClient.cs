using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Http;
using Fusion.Integration.Org;
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
        if (OrgClientRequestHeadersScope.Current.Value != null)
        {
            foreach (var header in OrgClientRequestHeadersScope.Current.Value.Headers)
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
            Content = new StringContent(requestContent, Encoding.UTF8, "application/json")
        };

        var response = await SendAsync(request, cancellationToken);
        return await RequestResponse<T>.FromResponseAsync(response);
    }

    public async Task<RequestResponse<TResponse>> PatchAsync<TResponse>(string url, object data)
    {
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
        request.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

        var response = await SendAsync(request);
        return await RequestResponse<TResponse>.FromResponseAsync(response);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);

        var response = await SendAsync(request);

        await response.ThrowIfUnsuccessfulAsync(content => { return new OrgApiError(response, content); });


        return response;
    }

    public async Task<RequestResponse<TResponse>> PutAsync<TResponse>(string url, TResponse data)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

        var response = await SendAsync(request);
        return await RequestResponse<TResponse>.FromResponseAsync(response);
    }

    public OrgClientRequestHeadersScope UseRequestHeaders()
    {
        return new OrgClientRequestHeadersScope();
    }
}