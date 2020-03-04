//using Castle.Core.Configuration;
//using Microsoft.Extensions.Configuration;
//using Microsoft.IdentityModel.Clients.ActiveDirectory;
//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Reflection;
//using System.Text;

//namespace Fusion.Resources.Domain.Tests
//{
//    class IntegrationTests
//    {
//    }

//    public static class SecretsHelper
//    {
//        private static IConfiguration config = null;

//        public static string GetUserSecret(string configName)
//        {
//            if (config is null)
//            {
//                var builder = new ConfigurationBuilder();
//                builder.AddUserSecrets(Assembly.GetExecutingAssembly());
//                config = builder.Build();
//            }

//            return config[configName];
//        }
//    }

//    public class ContextResolverFactory
//    {
//        private IHttpClientFactory httpClientFactory = null;
//        private ILogger<FusionContextResolver> logger = null;

//        private Dictionary<IFusionContextResolver, DefaultContextResolverCache> caches = new Dictionary<IFusionContextResolver, DefaultContextResolverCache>();


//        public async Task InitializeAsync()
//        {
//            var loggerMock = new Mock<ILogger<FusionContextResolver>>();
//            logger = loggerMock.Object;

//            if (httpClientFactory is null)
//            {
//                var url = SecretsHelper.GetUserSecret("Context:Endpoint");
//                var resource = SecretsHelper.GetUserSecret("Context:Resource");

//                var clientId = SecretsHelper.GetUserSecret("AzureAd:ClientId");
//                var secret = SecretsHelper.GetUserSecret("AzureAd:ClientSecret");
//                var authority = SecretsHelper.GetUserSecret("AzureAd:Authority");
//                var authContext = new AuthenticationContext(authority);

//                var token = await authContext.AcquireTokenAsync(resource, new ClientCredential(clientId, secret));

//                var client = new HttpClient
//                {
//                    BaseAddress = new Uri(url)
//                };
//                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token.AccessToken);

//                var mock = new Mock<IHttpClientFactory>();
//                mock.Setup(c => c.CreateClient(HttpClientNames.ApplicationContext)).Returns(client);

//                httpClientFactory = mock.Object;
//            }
//        }


//        public IFusionContextResolver CreateOfflineResolver(IContextResolverCache cache)
//        {
//            var offlineHttpClient = new HttpClient(new OfflineHttpHandler());
//            var httpClientFactory = new Mock<IHttpClientFactory>();
//            httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(offlineHttpClient);

//            var resolver = new FusionContextResolver(httpClientFactory.Object, cache, logger);
//            return resolver;
//        }

//        public IFusionContextResolver CreateResolver()
//        {
//            var cache = new DefaultContextResolverCache();
//            var resolver = new FusionContextResolver(httpClientFactory, cache, logger);

//            caches[resolver] = cache;

//            return resolver;
//        }
//        public IFusionContextResolver CreateResolver(IContextResolverCache cache)
//        {
//            var resolver = new FusionContextResolver(httpClientFactory, cache, logger);
//            return resolver;
//        }

//        public DefaultContextResolverCache GetCacheFor(IFusionContextResolver resolver) => caches[resolver];

//    }

//}
