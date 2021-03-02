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

namespace Fusion.Resources.Api.Tests.Fixture
{
    public class ResourcesApiWebAppFactory : WebApplicationFactory<Startup>
    {
        public readonly PeopleServiceMock peopleServiceMock;
        public readonly OrgServiceMock orgServiceMock;
        public readonly ContextResolverMock contextResolverMock;
        internal readonly RolesClientMock roleClientMock;

        private string resourceDbConnectionString = TestDbConnectionStrings.LocalDb($"resources-app-{DateTime.Now:yyyy-MM-dd-HHmmss}-{Guid.NewGuid()}");

        public ResourcesApiWebAppFactory()
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

            Environment.SetEnvironmentVariable("INTEGRATION_TEST_RUN", "true");
            Environment.SetEnvironmentVariable("AzureAd__ClientId", TestConstants.APP_CLIENT_ID);
            Environment.SetEnvironmentVariable("FORWARD_JWT", "True");
            Environment.SetEnvironmentVariable("FORWARD_COOKIE", "True");

            peopleServiceMock = new PeopleServiceMock();
            orgServiceMock = new OrgServiceMock();
            contextResolverMock = new ContextResolverMock();
            roleClientMock = new RolesClientMock();

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
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            lock (locker)
            {
                var fld = typeof(OrgConfigurationExtensions).GetField("hasAddedServices", BindingFlags.NonPublic | BindingFlags.Static);
                fld.SetValue(null, false);

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

                    //make it transient in the tests, to make sure that test contracts are added to in-memory collection
                    services.AddTransient<ICompanyResolver, PeopleCompanyResolver>();
                    services.AddSingleton<IProjectOrgResolver>(sp => new OrgResolverMock());
                    services.AddSingleton<IFusionContextResolver>(sp => contextResolverMock);
                    services.AddSingleton<IFusionRolesClient>(Span => roleClientMock);
                    services.AddSingleton<IFusionNotificationClient, NotificationClientMock>();

                    services.AddSingleton(sp =>
                    {
                        var clientFactoryMock = new Mock<IHttpClientFactory>();

                        clientFactoryMock.Setup(cfm => cfm.CreateClient(Fusion.Integration.Http.HttpClientNames.DelegatedPeople)).Returns(peopleServiceMock.CreateHttpClient());
                        clientFactoryMock.Setup(cfm => cfm.CreateClient(Fusion.Integration.Http.HttpClientNames.ApplicationPeople)).Returns(peopleServiceMock.CreateHttpClient());
                        clientFactoryMock.Setup(cfm => cfm.CreateClient(Fusion.Integration.Org.OrgConstants.HttpClients.Application)).Returns(orgServiceMock.CreateHttpClient());

                        return clientFactoryMock.Object;
                    });
                });
            }
        }
    }
}
