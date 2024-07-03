using Fusion.Summary.Api.Tests.Fixture;

namespace Fusion.Summary.Api.Tests.IntegrationTests;

[Collection(TestCollections.SUMMARY)]
public class SummaryReportTests
{
    private readonly SummaryApiFixture _fixture;
    private HttpClient _client;

    public SummaryReportTests(SummaryApiFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.GetClient();
    }

}