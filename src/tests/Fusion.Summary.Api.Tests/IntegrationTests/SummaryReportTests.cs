using FluentAssertions;
using Fusion.Summary.Api.Authorization;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Tests.Fixture;
using Fusion.Summary.Api.Tests.Helpers;
using Fusion.Summary.Api.Tests.IntegrationTests.Base;
using Fusion.Testing;
using Fusion.Testing.Models;
using Xunit.Abstractions;

namespace Fusion.Summary.Api.Tests.IntegrationTests;

[Collection(TestCollections.SUMMARY)]
public class SummaryReportTests : TestBase
{
    private readonly SummaryApiFixture _fixture;
    private HttpClient _client;

    public SummaryReportTests(SummaryApiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = _fixture.GetClient();
        SetOutput(output);
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
        response.Should().BeCreated();

        var getResponse = await _client.GetWeeklySummaryReportsAsync(department.DepartmentSapId);
        getResponse.Should().BeSuccessfull();
        getResponse.Value!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task PutAndUpdateWeeklySummaryReport_ShouldReturnNoContent()
    {
        using var adminScope = _fixture.AdminScope();
        var testUser = _fixture.Fusion.CreateUser().AsEmployee().AzureUniqueId!.Value;

        var department = await _client.PutDepartmentAsync(testUser);

        var response = await _client.PutWeeklySummaryReportAsync(department.DepartmentSapId);
        response.Should().BeCreated();

        response = await _client.PutWeeklySummaryReportAsync(department.DepartmentSapId);
        response.Should().BeNoContent();
    }

    [Fact]
    public async Task PutWeeklySummaryReport_WithInvalidPeriodDate_ShouldReturnBadRequest()
    {
        using var adminScope = _fixture.AdminScope();
        var testUser = _fixture.Fusion.CreateUser().AsEmployee().AzureUniqueId!.Value;

        var department = await _client.PutDepartmentAsync(testUser);

        var response = await _client.PutWeeklySummaryReportAsync(department.DepartmentSapId, (report) =>
        {
            var nowDate = DateTime.UtcNow;
            if (nowDate.DayOfWeek == DayOfWeek.Monday)
                nowDate = nowDate.AddDays(1);
            report.Period = nowDate;
        });

        response.Should().BeBadRequest();
    }

    [Theory]
    [InlineData("Fusion.LineOrg.Manager")]
    [InlineData("Fusion.Resources.ResourceOwner")]
    public async Task GetWeeklySummaryReports_AsResourceOwners_ShouldReturnSuccess(string role)
    {
        _fixture.Fusion.DataStore.Add(BogusGenerator.CreateOrgUnit().Generate());

        var sapId = _fixture.Fusion.DataStore.OrgUnits.First().SapId;

        var resourceOwner = _fixture.Fusion.CreateUser().AsResourceOwner().WithRole(role, userRole => { userRole.WithScope("OrgUnit", sapId); });

        ApiDepartment department;
        using (_fixture.AdminScope())
        {
            department = await _client.PutDepartmentAsync(resourceOwner.AzureUniqueId!.Value, s => { s.DepartmentSapId = sapId; });
        }

        using var resourceOwnerScope = _fixture.UserScope(resourceOwner);

        var response = await _client.GetWeeklySummaryReportsAsync(department.DepartmentSapId);
        response.Should().BeSuccessfull();
    }

    [Theory]
    [InlineData("AAA", true)]           // parent
    [InlineData("AAA CCC", true)]       // sibling
    [InlineData("BBB", false)]          // sibling of parent
    [InlineData("BBB CCC", false)]      // descendant of parent sibling
    [InlineData("AAA BBB CCC", false)]  // descendant
    public async Task GetWeeklySummaryReports_AsRelatedResourceOwners(string relatedDepartmentName, bool shouldSucceed)
    {
        var departmentName = "AAA BBB";

        using var adminScope = _fixture.AdminScope();

        var resourceOwner = _fixture.Fusion.CreateUser().AsEmployee().AzureUniqueId!.Value;
        var department = await _client.PutDepartmentAsync(resourceOwner, d => d.FullDepartmentName = departmentName);

        var relatedResourceOwner = _fixture.Fusion.CreateUser().AsEmployee();
        var relatedDepartment = await _client.PutDepartmentAsync(relatedResourceOwner.AzureUniqueId!.Value, d => d.FullDepartmentName = relatedDepartmentName);
        relatedResourceOwner.AsResourceOwner()
            .WithRole(
                "Fusion.Resources.ResourceOwner",
                userRole => { userRole.WithScope("OrgUnit", relatedDepartment.DepartmentSapId); })
            // add claims for claims transformation
            .WithClaim(ResourcesClaimTypes.ResourceOwnerForDepartment, relatedDepartment.FullDepartmentName);

        using var relatedResourceOwnerScope = _fixture.UserScope(relatedResourceOwner);

        var response = await _client.GetWeeklySummaryReportsAsync(department.DepartmentSapId);
        if (shouldSucceed)
            response.Should().BeSuccessfull();
        else
            response.Should().BeUnauthorized();
    }


    [Fact]
    public async Task GetLatestWeeklySummaryReport_ReturnsReportForThisWeek()
    {
        using var adminScope = _fixture.AdminScope();
        var testUser = _fixture.Fusion.CreateUser().AsEmployee().AzureUniqueId!.Value;

        var department = await _client.PutDepartmentAsync(testUser);

        var oldReportDate = DateUtils.GetPreviousWeeksMonday(DateTime.UtcNow.AddDays(-7));
        var newestReportDate = DateUtils.GetPreviousWeeksMonday(DateTime.UtcNow);


        var response = await _client.PutWeeklySummaryReportAsync(department.DepartmentSapId, s => { s.Period = oldReportDate; });
        response.Should().BeCreated();

        var getResponse = await _client.GetLatestWeeklySummaryReportAsync(department.DepartmentSapId);
        getResponse.Should().BeNotFound();

        response = await _client.PutWeeklySummaryReportAsync(department.DepartmentSapId, s => { s.Period = newestReportDate; });
        response.Should().BeCreated();

        getResponse = await _client.GetLatestWeeklySummaryReportAsync(department.DepartmentSapId);
        getResponse.Should().BeSuccessfull();
        getResponse.Value.Should().NotBeNull();
    }
}