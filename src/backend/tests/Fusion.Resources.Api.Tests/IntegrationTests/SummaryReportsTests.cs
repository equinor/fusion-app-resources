using System.Threading.Tasks;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests;

[Collection("Integration")]
public class SummaryReportsTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
{
    private ResourceApiFixture fixture;
    private TestLoggingScope loggingScope;

    public SummaryReportsTests(ResourceApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;

        // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
        loggingScope = new TestLoggingScope(output);
    }

    [Fact]
    public async Task GetSummaryReport_GetLatest_ShouldReturnLatestReport()
    {
    }

    public Task InitializeAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task DisposeAsync()
    {
        loggingScope.Dispose();
        return Task.CompletedTask;
    }
}