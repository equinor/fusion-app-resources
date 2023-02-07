using Fusion.Events;
using Fusion.Integration;
using Fusion.Integration.Notification;
using Fusion.Integration.Org;
using Fusion.Integration.Roles;
using Fusion.Integration.ServiceDiscovery;
using Fusion.Resources.Api.Tests.FusionMocks;
using Fusion.Resources.Database;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Services;
using Fusion.Testing;
using Fusion.Testing.Mocks.ContextService;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.OrgService.Resolvers;
using Fusion.Testing.Mocks.ProfileService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests.Fixture
{
    public class ResourcesApiWebAppFactory : WebApplicationFactory<Startup>
    {
        public readonly LineOrgServiceMock lineOrgServiceMock;
        public readonly PeopleServiceMock peopleServiceMock;
        public readonly OrgServiceMock orgServiceMock;
        public readonly ContextResolverMock contextResolverMock;
        internal readonly RolesClientMock roleClientMock;

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

            lineOrgServiceMock = new LineOrgServiceMock();
            peopleServiceMock = new PeopleServiceMock();
            orgServiceMock = new OrgServiceMock();
            contextResolverMock = new ContextResolverMock();
            roleClientMock = new RolesClientMock();
            queueMock = new Mock<IQueueSender>();
            queueMock.Setup(c => c.SendMessageAsync(It.IsAny<QueuePath>(), It.IsAny<object>())).Returns(Task.CompletedTask);
            queueMock.Setup(c => c.SendMessageDelayedAsync(It.IsAny<QueuePath>(), It.IsAny<object>(), It.IsAny<int>())).Returns(Task.CompletedTask);

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
        public bool IsMemorycacheDisabled { get; set; } = false;

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
                services.TryRemoveImplementationService("FusionServiceDiscoveryHostedService");
                services.TryRemoveImplementationService("InfrastructureInitialization");
                
                services.TryRemoveImplementationService("PeopleEventReceiver");
                services.TryRemoveImplementationService("OrgEventReceiver");
                services.TryRemoveImplementationService("ContextEventReceiver");
                services.TryRemoveImplementationService<ICompanyResolver>();

                if (IsMemorycacheDisabled)
                {
                    services.TryRemoveImplementationService<IMemoryCache>();
                    services.AddSingleton<IMemoryCache, AlwaysEmptyCache>();
                }
                services.AddSingleton(new Mock<IFusionServiceDiscovery>(MockBehavior.Loose).Object);
                //make it transient in the tests, to make sure that test contracts are added to in-memory collection
                services.AddTransient<ICompanyResolver, PeopleCompanyResolver>();
                services.AddSingleton<IProjectOrgResolver>(sp => new OrgResolverMock());
                services.AddSingleton<IFusionContextResolver>(sp => contextResolverMock);
                services.AddSingleton<IFusionRolesClient>(Span => roleClientMock);
                services.AddSingleton<IFusionNotificationClient, NotificationClientMock>();
                services.AddTransient<IQueueSender>(sp => queueMock.Object);
                services.AddSingleton<IEventNotificationClient>(sp => new EventNotificationClientMock(new TestMessageBus(), "resources-sub"));
                services.AddSingleton<IPeopleIntegration, PeopleIntegrationMock>();
                services.AddSingleton(sp =>
                {
                    var clientFactoryMock = new Mock<IHttpClientFactory>();
                    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();

                    clientFactoryMock.Setup(cfm => cfm.CreateClient(IntegrationConfig.HttpClients.ApplicationLineOrg())).Returns(lineOrgServiceMock.CreateHttpClient());
                    clientFactoryMock.Setup(cfm => cfm.CreateClient(Fusion.Integration.Http.HttpClientNames.DelegatedPeople)).Returns(peopleServiceMock.CreateHttpClient());
                    clientFactoryMock.Setup(cfm => cfm.CreateClient(Fusion.Integration.Http.HttpClientNames.ApplicationPeople)).Returns(peopleServiceMock.CreateHttpClient());
                    clientFactoryMock.Setup(cfm => cfm.CreateClient(Fusion.Integration.Org.OrgConstants.HttpClients.Application)).Returns(orgServiceMock.CreateHttpClient());

                    clientFactoryMock.Setup(cfm => cfm.CreateClient(Fusion.Integration.Org.OrgConstants.HttpClients.Delegate)).Returns(() =>
                    {
                        var currentUser = httpContextAccessor.HttpContext?.User.GetAzureUniqueId();

                        var client = orgServiceMock.CreateHttpClient();
                        client.DefaultRequestHeaders.Add("x-fusion-test-delegated", $"{currentUser}");
                        return client;
                    });

                    return clientFactoryMock.Object;
                });
            });
        }
    }
}
