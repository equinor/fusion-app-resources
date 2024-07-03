using Fusion.Summary.Api.Tests.Fixture;

namespace Fusion.Summary.Api.Tests.IntegrationTests;

[Collection(TestCollections.SUMMARY)]
public class SummaryReportTests
{
    private readonly SummaryApiFixture _fixture;

    public SummaryReportTests(SummaryApiFixture fixture)
    {
        _fixture = fixture;
    }


    [Fact]
    public async Task GetSummaryReport_ReturnsSummaryReport()
    {
        Assert.True(true);
    }
}