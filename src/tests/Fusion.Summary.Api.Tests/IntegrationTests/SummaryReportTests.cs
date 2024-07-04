using FluentAssertions;
using Fusion.Summary.Api.Tests.Fixture;
using Fusion.Summary.Api.Tests.Helpers;
using Fusion.Testing;

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


    [Fact]
    public async Task GetReportForMissingDepartment_ShouldReturnNotFound()
    {
        using var adminScope = _fixture.AdminScope();

        var nonExistingSapId = "FCA34128-BEA6-4238-B848-D749546F1E2E";

        var response = await _client.GetWeeklySummaryReportsAsync(nonExistingSapId);
        response.Should().BeNotFound();
    }

    [Fact]
    public async Task PutAndGetWeeklySummaryReport_ShouldReturnReport()
    {
        using var adminScope = _fixture.AdminScope();
        var testUser = _fixture.Fusion.CreateUser().AsEmployee().AzureUniqueId!.Value;

        var department = await _client.PutDepartmentAsync(testUser);


        var response = await _client.PutWeeklySummaryReportAsync(department.DepartmentSapId);
        response.Should().BeNoContent();

        var getResponse = await _client.GetWeeklySummaryReportsAsync(department.DepartmentSapId);
        getResponse.Should().BeSuccessfull();
        getResponse.Value!.Items.Should().HaveCount(1);
    }

}