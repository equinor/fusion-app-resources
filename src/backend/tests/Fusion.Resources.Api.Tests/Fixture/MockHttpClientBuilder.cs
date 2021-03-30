using Moq;
using Moq.Protected;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests.Fixture
{
    public class MockHttpClientBuilder
    {
        public record StubResponse(string Url, string Content, HttpStatusCode StatusCode = HttpStatusCode.OK);
        class MockApi : KeyedCollection<string, StubResponse>
        {
            protected override string GetKeyForItem(StubResponse item) => item.Url;
        }

        private const string baseaddress = "http://mock-api.local";
        private readonly MockApi stubs = new MockApi();

        public void WithResponse(StubResponse response) => stubs.Add(response);
        public void WithResponse(string url, string content, HttpStatusCode statusCode = HttpStatusCode.OK)
            => WithResponse(new StubResponse(url, content, statusCode));
        public void WithResponse<T>(string url, T data, HttpStatusCode statusCode = HttpStatusCode.OK)
            => WithResponse(url, JsonSerializer.Serialize(data), statusCode);

        public HttpClient Build()
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync((HttpRequestMessage x, CancellationToken _) =>
                {
                    var response = stubs[x.RequestUri.LocalPath];
                    return new HttpResponseMessage
                    {
                        StatusCode = response.StatusCode,
                        Content = new StringContent(response.Content)
                    };
                });

            return new HttpClient(handler.Object) { BaseAddress = new Uri(baseaddress) };
        }

        public void Reset() => stubs.Clear();
    }
}
