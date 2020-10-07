using Fusion.Integration.Profile.ApiClient;
using Fusion.Testing.Authentication.User;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Testing
{
    public class TestClientHttpResponse<TResp>
    {
        public string Content { get; set; }
        public HttpResponseMessage Response { get; set; }
        public string HttpReason { get; set; }

        public TResp Value { get; set; }

        public TestClientHttpResponse(HttpResponseMessage response)
        {
            Response = response;
            HttpReason = response.ReasonPhrase;
        }

        public static async Task<TestClientHttpResponse<T>> CreateResponseAsync<T>(HttpResponseMessage response, bool skipDeserialization = false)
        {
            var respObject = new TestClientHttpResponse<T>(response)
            {
                Content = await response.Content.ReadAsStringAsync()
            };

            try
            {
                if (skipDeserialization && typeof(T) == typeof(string))
                    respObject.Value = (T)(object)respObject.Content;
                else
                    respObject.Value = JsonConvert.DeserializeObject<T>(respObject.Content);
            }
            catch (Exception ex)
            {
                TestLogger.TryLog($"Unable to deserialize to type '{typeof(T).Name}'");
                TestLogger.TryLog(ex.ToString());
            }

            return respObject;
        }

        public static async Task<TestClientHttpResponse<T>> CreateResponseAsync<T>(HttpResponseMessage response, T anonymousType)
        {
            var respObject = new TestClientHttpResponse<T>(response)
            {
                Content = await response.Content.ReadAsStringAsync()
            };

            try
            {
                respObject.Value = JsonConvert.DeserializeAnonymousType<T>(respObject.Content, anonymousType);
            }
            catch (Exception ex)
            {
                TestLogger.TryLog($"Unable to deserialize to type '{typeof(T).Name}'");
                TestLogger.TryLog(ex.ToString());
            }

            return respObject;
        }
    }

    public class TestClientScope : IDisposable
    {
        private static AsyncLocal<ApiPersonProfileV3> CurrentUser = new AsyncLocal<ApiPersonProfileV3>();
        private static AsyncLocal<Guid?> CurrentAppId = new AsyncLocal<Guid?>();
        private static AsyncLocal<List<KeyValuePair<string, string>>> CurrentHeaders = new AsyncLocal<List<KeyValuePair<string, string>>>();

        public TestClientScope(ApiPersonProfileV3 profile)
        {
            CurrentUser.Value = profile;
        }
        public TestClientScope(string name, string value)
        {
            AddHeader(name, value);
        }

        public TestClientScope SetUser(ApiPersonProfileV3 profile)
        {
            CurrentUser.Value = profile;
            return this;
        }
        public TestClientScope SetSigninAppId(Guid? appId)
        {
            CurrentAppId.Value = appId;
            return this;
        }

        public TestClientScope AddHeader(string name, string value)
        {

            if (CurrentHeaders.Value == null)
            {
                CurrentHeaders.Value = new List<KeyValuePair<string, string>>();
            }
            CurrentHeaders.Value.Add(new KeyValuePair<string, string>(name, value));

            return this;
        }

        public void Dispose()
        {
            CurrentUser.Value = null;
            CurrentHeaders.Value = null;
            CurrentAppId.Value = null;
        }

        public static void AddHeaders(HttpRequestMessage message)
        {
            if (CurrentHeaders.Value != null)
            {
                foreach (var header in CurrentHeaders.Value)
                    message.Headers.Add(header.Key, header.Value);
            }

            if (CurrentUser.Value != null)
            {
                message.AddTestUserToken(CurrentUser.Value, CurrentAppId.Value);
            }
        }
    }

    public static class TestHttpClientExtensions
    {
        #region GET
        public static async Task<TestClientHttpResponse<TResp>> TestClientGetAsync<TResp>(this HttpClient client, string requestUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            TestClientScope.AddHeaders(request);

            var resp = await client.SendAsync(request);
            var respObj = await TestClientHttpResponse<TResp>.CreateResponseAsync<TResp>(resp);

            TestLogger.TryLog($"GET {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
                TestLogger.TryLog(respObj.Content);

            return respObj;
        }
        public static async Task<TestClientHttpResponse<TResp>> TestClientGetAsync<TResp>(this HttpClient client, string requestUri, TResp returnType)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            TestClientScope.AddHeaders(request);

            var resp = await client.SendAsync(request);
            var respObj = await TestClientHttpResponse<TResp>.CreateResponseAsync<TResp>(resp, returnType);

            TestLogger.TryLog($"GET {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
                TestLogger.TryLog(respObj.Content);

            return respObj;
        }

        public static async Task<TestClientHttpResponse<string>> TestClientGetStringAsync(this HttpClient client, string requestUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            TestClientScope.AddHeaders(request);

            var resp = await client.SendAsync(request);
            var respObj = await TestClientHttpResponse<string>.CreateResponseAsync<string>(resp, skipDeserialization: true);

            TestLogger.TryLog($"GET {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
                TestLogger.TryLog(respObj.Content);

            return respObj;
        }
        #endregion

        #region POST
        public static async Task<TestClientHttpResponse<T>> TestClientPostAsync<T>(this HttpClient client, string requestUri, object value)
        {
            string content = JsonConvert.SerializeObject(value, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            StringContent stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = stringContent;

            TestClientScope.AddHeaders(request);

            var resp = await client.SendAsync(request);
            var respObj = await TestClientHttpResponse<T>.CreateResponseAsync<T>(resp);

            TestLogger.TryLog($"POST {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
                TestLogger.TryLog($"Request payload: {content}");
            }

            return respObj;
        }

        public static async Task<TestClientHttpResponse<dynamic>> TestClientPostAsync(this HttpClient client, string requestUri, object value)
        {
            string content = JsonConvert.SerializeObject(value, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            StringContent stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = stringContent;

            TestClientScope.AddHeaders(request);

            var resp = await client.SendAsync(request);
            var respObj = await TestClientHttpResponse<dynamic>.CreateResponseAsync<dynamic>(resp);

            TestLogger.TryLog($"POST {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
                TestLogger.TryLog($"Request payload: {content}");
            }

            return respObj;
        }

        public static async Task<TestClientHttpResponse<T>> TestClientPostAsync<T>(this HttpClient client, string requestUri, object value, T responseType)
        {
            string content = JsonConvert.SerializeObject(value, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            StringContent stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = stringContent;

            TestClientScope.AddHeaders(request);

            var resp = await client.SendAsync(request);
            var respObj = await TestClientHttpResponse<T>.CreateResponseAsync<T>(resp, responseType);

            TestLogger.TryLog($"POST {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
                TestLogger.TryLog($"Request payload: {content}");
            }

            return respObj;
        }

        public static async Task<TestClientHttpResponse<T>> TestClientPostNoPayloadAsync<T>(this HttpClient client, string requestUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

            TestClientScope.AddHeaders(request);

            var resp = await client.SendAsync(request);
            var respObj = await TestClientHttpResponse<T>.CreateResponseAsync<T>(resp);

            TestLogger.TryLog($"POST {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
            }

            return respObj;
        }

        public static async Task<TestClientHttpResponse<T>> TestClientPostNoPayloadAsync<T>(this HttpClient client, string requestUri, T returnType)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

            TestClientScope.AddHeaders(request);

            var resp = await client.SendAsync(request);
            var respObj = await TestClientHttpResponse<T>.CreateResponseAsync<T>(resp, returnType);

            TestLogger.TryLog($"POST {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
            }

            return respObj;
        }

        public static async Task<TestClientHttpResponse<TResp>> TestClientPostMultipartAsync<TResp>(this HttpClient client, string requestUri, MultipartFormDataContent form, TResp respType)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = form;

            TestClientScope.AddHeaders(request);

            var resp = await client.SendAsync(request);
            var respObj = await TestClientHttpResponse<TResp>.CreateResponseAsync<TResp>(resp, respType);

            TestLogger.TryLog($"POST {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                TestLogger.TryLog(respObj.Content);
                TestLogger.TryLog($"Request payload: -- form data --");
            }

            return respObj;
        }

        public static async Task<TestClientHttpResponse<TResponse>> TestClientPostFileAsync<TResponse>(this HttpClient client, string requestUri, Stream documentStream, string contentType, TResponse respType)
        {

            using (var streamContent = new StreamContent(documentStream))
            {
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                streamContent.Headers.ContentLength = documentStream.Length;

                var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                request.Content = streamContent;
                TestClientScope.AddHeaders(request);

                var resp = await client.SendAsync(request);
                var respObj = await TestClientHttpResponse<TResponse>.CreateResponseAsync<TResponse>(resp, respType);

                TestLogger.TryLog($"POST {resp.RequestMessage.RequestUri} [CT: {contentType}] -> {resp.StatusCode}");
                if (!resp.IsSuccessStatusCode)
                {
                    var respContent = await resp.Content.ReadAsStringAsync();
                    TestLogger.TryLog(respContent);
                }

                return respObj;
            }
        }

        #endregion

        #region PATCH

        public static async Task<TestClientHttpResponse<TResponse>> PatchAsJsonAsync<TResponse>(this HttpClient client, string requestUri, object value)
        {
            string content = JsonConvert.SerializeObject(value);
            StringContent stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Patch, requestUri)
            {
                Content = stringContent
            };

            TestClientScope.AddHeaders(message);

            var resp = await client.SendAsync(message);
            var respObj = await TestClientHttpResponse<TResponse>.CreateResponseAsync<TResponse>(resp);

            TestLogger.TryLog($"PATCH {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
                TestLogger.TryLog($"Request payload: {content}");
            }

            return respObj;
        }

        public static async Task<TestClientHttpResponse<TResponse>> TestClientPatchAsync<TResponse>(this HttpClient client, string requestUri, object value, TResponse response)
        {
            string content = JsonConvert.SerializeObject(value);
            StringContent stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Patch, requestUri)
            {
                Content = stringContent
            };

            TestClientScope.AddHeaders(message);

            var resp = await client.SendAsync(message);
            var respObj = await TestClientHttpResponse<TResponse>.CreateResponseAsync<TResponse>(resp, response);

            TestLogger.TryLog($"PATCH {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
                TestLogger.TryLog($"Request payload: {content}");
            }

            return respObj;
        }

        #endregion

        #region PUT

        public static async Task<TestClientHttpResponse<TResponse>> TestClientPutAsync<TResponse>(this HttpClient client, string requestUri, object value)
        {
            string content = JsonConvert.SerializeObject(value);
            StringContent stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = stringContent
            };

            TestClientScope.AddHeaders(message);

            var resp = await client.SendAsync(message);
            var respObj = await TestClientHttpResponse<TResponse>.CreateResponseAsync<TResponse>(resp);

            TestLogger.TryLog($"PUT {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
                TestLogger.TryLog($"Request payload: {content}");
            }

            return respObj;
        }

        public static async Task<TestClientHttpResponse<TResponse>> TestClientPutAsync<TResponse>(this HttpClient client, string requestUri, object value, TResponse response)
        {
            string content = JsonConvert.SerializeObject(value);
            StringContent stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = stringContent
            };
            
            TestClientScope.AddHeaders(message);

            var resp = await client.SendAsync(message);
            var respObj = await TestClientHttpResponse<TResponse>.CreateResponseAsync<TResponse>(resp, response);

            TestLogger.TryLog($"PUT {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
                TestLogger.TryLog($"Request payload: {content}");
            }

            return respObj;
        }

        #endregion

        #region DELETE

        public static async Task<TestClientHttpResponse<TResponse>> TestClientDeleteAsync<TResponse>(this HttpClient client, string requestUri)
        {
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            TestClientScope.AddHeaders(message);

            var resp = await client.SendAsync(message);
            var respObj = await TestClientHttpResponse<TResponse>.CreateResponseAsync<TResponse>(resp);

            TestLogger.TryLog($"DELETE {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
            }

            return respObj;
        }

        public static async Task<TestClientHttpResponse<TResponse>> TestClientDeleteAsync<TResponse>(this HttpClient client, string requestUri, TResponse response)
        {
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            TestClientScope.AddHeaders(message);

            var resp = await client.SendAsync(message);
            var respObj = await TestClientHttpResponse<TResponse>.CreateResponseAsync<TResponse>(resp, response);

            TestLogger.TryLog($"DELETE {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
            }

            return respObj;
        }

        public static async Task<TestClientHttpResponse<dynamic>> TestClientDeleteAsync(this HttpClient client, string requestUri)
        {
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, requestUri);

            TestClientScope.AddHeaders(message);

            var resp = await client.SendAsync(message);
            var respObj = await TestClientHttpResponse<dynamic>.CreateResponseAsync<dynamic>(resp);

            TestLogger.TryLog($"DELETE {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
            }

            return respObj;
        }

        #endregion

        #region OPTIONS

        public static async Task<TestClientHttpResponse<dynamic>> TestClientOptionsAsync(this HttpClient client, string requestUri)
        {
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Options, requestUri);

            TestClientScope.AddHeaders(message);

            var resp = await client.SendAsync(message);
            var respObj = await TestClientHttpResponse<dynamic>.CreateResponseAsync<dynamic>(resp);

            TestLogger.TryLog($"PATCH {resp.RequestMessage.RequestUri} -> {resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                var respContent = await resp.Content.ReadAsStringAsync();
                TestLogger.TryLog(respContent);
            }

            return respObj;
        }

        #endregion
    }
}
