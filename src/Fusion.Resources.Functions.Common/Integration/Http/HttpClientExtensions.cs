using Fusion.Resources.Functions.Common.Integration.Errors;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Common.Integration.Http
{
    public static class HttpClientExtensions
    {
        public static async Task<T> GetAsJsonAsync<T>(this HttpClient client, string url) where T : class
        {
            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new ApiError(response.RequestMessage!.RequestUri!.ToString(), response.StatusCode, body, "Response from API call indicates error");

            T deserialized = JsonConvert.DeserializeObject<T>(body);
            return deserialized;
        }

        public static async Task<IEnumerable<string>> OptionsAsync(this HttpClient client, string url)
        {
            var message = new HttpRequestMessage(HttpMethod.Options, url);
            var resp = await client.SendAsync(message);

            resp.Content.Headers.TryGetValues("Allow", out var allowHeaders);

            return allowHeaders;
        }

    }
}