using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Fusion.Resources.Domain.Services.OrgClient;

public class RequestResponse<TResponse>
{
    private RequestResponse(HttpResponseMessage response, string content, TResponse value)
    {
        Value = value;
        Content = content;
        Response = response;
    }

    private RequestResponse(HttpResponseMessage response, string content)
    {
        Content = content;
        Response = response;
        Value = default(TResponse);
    }

    public HttpStatusCode StatusCode => Response.StatusCode;
    public bool IsSuccessStatusCode => Response.IsSuccessStatusCode;

    public string Content { get; }
    public TResponse Value { get; }
    public HttpResponseMessage Response { get; }

    public static async Task<RequestResponse<TResponse>> FromResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var value = JsonConvert.DeserializeObject<TResponse>(content);

            return new RequestResponse<TResponse>(response, content, value);
        }

        return new RequestResponse<TResponse>(response, content);
    }
}