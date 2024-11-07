using FluentAssertions;
using Fusion.Summary.Api.Tests.Fixture;
using Fusion.Summary.Api.Tests.Helpers;
using Fusion.Summary.Api.Tests.IntegrationTests.Base;
using Fusion.Testing;
using Xunit.Abstractions;

namespace Fusion.Summary.Api.Tests.IntegrationTests;

[Collection(TestCollections.SUMMARY)]
public class WeeklyTaskOwnerReportTests : TestBase
{
    private readonly SummaryApiFixture _fixture;
    private HttpClient _client;

    public WeeklyTaskOwnerReportTests(SummaryApiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = _fixture.GetClient();
        SetOutput(output);
    }


    [Fact]
    public async Task PutAndGetWeeklyTaskOwnerReport_ShouldReturnReport()
    {
        using var adminScope = _fixture.AdminScope();
        var testUser = _fixture.Fusion.CreateUser().AsEmployee().AzureUniqueId!.Value;

        var projectResponse = await _client.PutProjectAsync(s => { s.DirectorAzureUniqueId = testUser; });
        projectResponse.Should().BeSuccessfull();
        var projectExternalId = projectResponse.Value!.OrgProjectExternalId.ToString();

        var response = await _client.PutWeeklyTaskOwnerReportAsync(projectExternalId);
        response.Should().BeSuccessfull();

        var getResponse = await _client.GetWeeklyTaskOwnerReportsAsync(projectExternalId);
        getResponse.Should().BeSuccessfull();
        getResponse.Value!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task PutWeeklyTaskOwnerReport_WithInvalidPeriodDate_ShouldReturnBadRequest()
    {
        using var adminScope = _fixture.AdminScope();
        var testUser = _fixture.Fusion.CreateUser().AsEmployee().AzureUniqueId!.Value;

        var projectResponse = await _client.PutProjectAsync(s => { s.DirectorAzureUniqueId = testUser; });
        projectResponse.Should().BeSuccessfull();
        var project = projectResponse.Value!;

        var reportResponse = await _client.PutWeeklyTaskOwnerReportAsync(project.OrgProjectExternalId.ToString(), (report) =>
        {
            var nowDate = DateTime.UtcNow;
            if (nowDate.DayOfWeek == DayOfWeek.Monday)
                nowDate = nowDate.AddDays(1);
            report.PeriodStart = nowDate;
        });

        reportResponse.Should().BeBadRequest();
    }
}