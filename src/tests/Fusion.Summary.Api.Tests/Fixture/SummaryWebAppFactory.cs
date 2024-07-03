using Fusion.Summary.Api.Database;
using Fusion.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;

namespace Fusion.Summary.Api.Tests.Fixture;

public class SummaryWebAppFactory : WebApplicationFactory<Program>
{
    private readonly FusionTestFixture _fusionFixture;

    private string dbConnectionString =
        TestDbConnectionStrings.LocalDb($"summary-app-{DateTime.Now:yyyy-MM-dd-HHmmss}-{Guid.NewGuid()}");

    public SummaryWebAppFactory(FusionTestFixture fusionFixture)
    {
        _fusionFixture = fusionFixture;

        IdentityModelEventSource.ShowPII = true;

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "IntegrationTesting");
        Environment.SetEnvironmentVariable("INTEGRATION_TEST_RUN", "true");
        Environment.SetEnvironmentVariable("AzureAd__ClientId", TestConstants.APP_CLIENT_ID);
        Environment.SetEnvironmentVariable("FORWARD_JWT", "True");
        Environment.SetEnvironmentVariable("FORWARD_COOKIE", "True");

        // Must set the config mode so the sql token generator does not try to refresh the access token, which kills the test run.
        Environment.SetEnvironmentVariable("Database__ConnectionMode", "Default");

        EnsureDatabaseCreated();
    }

    private void EnsureDatabaseCreated()
    {
        var services = new ServiceCollection();
        services.AddDbContext<SummaryDbContext>(options => { options.UseSqlServer(dbConnectionString); });

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SummaryDbContext>();

        // Migration logic will be handled by the API itself
        dbContext.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(cfgBuilder =>
        {
            cfgBuilder.AddInMemoryCollection(new Dictionary<string, string?>()
            {
                { $"ConnectionStrings:{nameof(SummaryDbContext)}", dbConnectionString }
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

            services.OverrideFusionContextIntegration(_fusionFixture);
            services.OverrideFusionHttpClientFactory(_fusionFixture);
        });
    }
}