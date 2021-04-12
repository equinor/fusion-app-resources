using Moq;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Fusion.Resources.Logic.Tests
{
    /// <summary>
    /// Helper to setup Mock, to respond to different http requests.
    /// </summary>
    public class MockRequest
    {
        public static HttpRequestMessage POST(string uri)
        {
            return It.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post && IsUri(r, uri));
        }

        private static bool IsUri(HttpRequestMessage request, string uri)
        {
            return Regex.IsMatch(request.RequestUri?.OriginalString ?? "", uri, RegexOptions.IgnoreCase);
        }
    }

}
