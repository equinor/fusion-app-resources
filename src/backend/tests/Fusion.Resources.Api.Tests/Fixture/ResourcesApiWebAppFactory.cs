using Fusion.Resources.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.TestHost;
using System.Net.Http;
using Moq;
using Fusion.Testing.Mocks.ProfileService;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Integration.Org;
using Fusion.Testing.Mocks.OrgService.Resolvers;
using Fusion.Testing.Mocks.ContextService;
using Fusion.Integration;
using Fusion.Resources.Api.Tests.FusionMocks;
using Fusion.Integration.Notification;
using Fusion.Integration.Roles;
using Fusion.Testing;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Services;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Fusion.Resources.Application.LineOrg;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Fusion.Resources.Api.Tests.Fixture
{
    public class ResourcesApiWebAppFactory : WebApplicationFactory<Startup>
    {
        public readonly PeopleServiceMock peopleServiceMock;
        public readonly OrgServiceMock orgServiceMock;
        public readonly ContextResolverMock contextResolverMock;
        internal readonly RolesClientMock roleClientMock;
        public readonly MockHttpClientBuilder lineOrgMock;

        public readonly Mock<IQueueSender> queueMock;

        private string resourceDbConnectionString = TestDbConnectionStrings.LocalDb($"resources-app-{DateTime.Now:yyyy-MM-dd-HHmmss}-{Guid.NewGuid()}");

        public ResourcesApiWebAppFactory()
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

            Environment.SetEnvironmentVariable("INTEGRATION_TEST_RUN", "true");
            Environment.SetEnvironmentVariable("AzureAd__ClientId", TestConstants.APP_CLIENT_ID);
            Environment.SetEnvironmentVariable("FORWARD_JWT", "True");
            Environment.SetEnvironmentVariable("FORWARD_COOKIE", "True");
            // Must set the config mode so the sql token generator does not try to refresh the access token, which kills the test run.
            Environment.SetEnvironmentVariable("Database__ConnectionMode", "Default");

            peopleServiceMock = new PeopleServiceMock();
            orgServiceMock = new OrgServiceMock();
            contextResolverMock = new ContextResolverMock();
            roleClientMock = new RolesClientMock();
            lineOrgMock = new MockHttpClientBuilder();
            queueMock = new Mock<IQueueSender>();
            queueMock.Setup(c => c.SendMessageAsync(It.IsAny<QueuePath>(), It.IsAny<object>())).Returns(Task.CompletedTask);

            EnsureDatabase();
        }

        private void EnsureDatabase()
        {
            var services = new ServiceCollection();
            services.AddDbContext<ResourcesDbContext>(options =>
            {
                options.UseSqlServer(resourceDbConnectionString);
            });

            using (var sp = services.BuildServiceProvider())
            using (var scope = sp.CreateScope())
            {
                ResourcesDbContext dbContext = scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();
                dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

                dbContext.Database.EnsureCreated();
            }
        }

        private static object locker = new object();
        public bool isMemorycacheDisabled = false;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(cfgBuilder =>
            {
                cfgBuilder.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { $"ConnectionStrings:{nameof(ResourcesDbContext)}", resourceDbConnectionString }
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddIntegrationTestingAuthentication();
                services.TryRemoveTransientEventHandlers();

                services.TryRemoveImplementationService("PeopleEventReceiver");
                services.TryRemoveImplementationService("OrgEventReceiver");
                services.TryRemoveImplementationService("ContextEventReceiver");
                services.TryRemoveImplementationService<ICompanyResolver>();
                services.TryRemoveImplementationService<LineOrgCacheRefresher>();
                
                if(isMemorycacheDisabled)
                {
                    services.TryRemoveImplementationService<IMemoryCache>();
                    services.AddScoped<IMemoryCache, AlwaysEmptyCache>();
                }

                //make it transient in the tests, to make sure that test contracts are added to in-memory collection
                services.AddTransient<ICompanyResolver, PeopleCompanyResolver>();
                services.AddSingleton<IProjectOrgResolver>(sp => new OrgResolverMock());
                services.AddSingleton<IFusionContextResolver>(sp => contextResolverMock);
                services.AddSingleton<IFusionRolesClient>(Span => roleClientMock);
                services.AddSingleton<IFusionNotificationClient, NotificationClientMock>();
                services.AddTransient<IQueueSender>(sp => queueMock.Object);

                services.AddSingleton(sp =>
                {
                    var clientFactoryMock = new Mock<IHttpClientFactory>();
                    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();

                    clientFactoryMock.Setup(cfm => cfm.CreateClient(Fusion.Integration.Http.HttpClientNames.DelegatedPeople)).Returns(peopleServiceMock.CreateHttpClient());
                    clientFactoryMock.Setup(cfm => cfm.CreateClient(Fusion.Integration.Http.HttpClientNames.ApplicationPeople)).Returns(peopleServiceMock.CreateHttpClient());
                    clientFactoryMock.Setup(cfm => cfm.CreateClient(Fusion.Integration.Org.OrgConstants.HttpClients.Application)).Returns(orgServiceMock.CreateHttpClient());
                    
                    clientFactoryMock.Setup(cfm => cfm.CreateClient(Fusion.Integration.Org.OrgConstants.HttpClients.Delegate)).Returns(() => {
                        var currentUser = httpContextAccessor.HttpContext?.User.GetAzureUniqueId();

                        var client = orgServiceMock.CreateHttpClient();
                        client.DefaultRequestHeaders.Add("x-fusion-test-delegated", $"{currentUser}");
                        return client;
                    });
                    clientFactoryMock.Setup(cfm => cfm.CreateClient("lineorg")).Returns(lineOrgMock.Build());

                    return clientFactoryMock.Object;
                });
            });
        }
    }
}
