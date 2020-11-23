using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Integration
{
    public static class HttpClientExtensions
    {
        public static async Task<T> GetAsJsonAsync<T>(this HttpClient client, string url) where T : class
        {
            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new IntegrationError(url, response.StatusCode);

            T deserialized = JsonConvert.DeserializeObject<T>(body);
            return deserialized;
        }

        public static async Task<T> GetAsJsonAsync<T>(this HttpClient client, string url, T returnAnonymousType) where T : class
        {
            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new IntegrationError(url, response.StatusCode);

            T deserialized = JsonConvert.DeserializeObject<T>(body);
            return deserialized;
        }
    }
}
