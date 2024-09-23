namespace Fusion.Resources.Functions.Common.Extensions;

public static class HttpResponseMessageExtensions
{
    public static async Task ThrowIfUnsuccessfulAsync<T>(this HttpResponseMessage response, Func<string, T> exFactory) where T : Exception
    {
        if (!response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadAsStringAsync();

            var ex = exFactory(data);
            throw ex;
        }
    }
}