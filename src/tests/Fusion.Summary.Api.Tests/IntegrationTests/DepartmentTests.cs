using FluentAssertions;
using Fusion.Summary.Api.Tests.Fixture;
using Fusion.Summary.Api.Tests.Helpers;
using Fusion.Testing;

namespace Fusion.Summary.Api.Tests.IntegrationTests;

[Collection(TestCollections.SUMMARY)]
public class DepartmentTests
{
    private readonly SummaryApiFixture _fixture;
    private HttpClient _client;

    public DepartmentTests(SummaryApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.GetClient();
    }

    [Fact]
    public async Task GetMissingDepartment_ShouldReturnNotFound()
    {
        using var adminScope = _fixture.AdminScope();

        var nonExistingSapId = "FCA34128-BEA6-4238-B848-D749546F1E2E";

        var response = await _client.GetDepartmentAsync(nonExistingSapId);
        response.Should().BeNotFound();
    }

    [Fact]
    public async Task PutDepartment_Then_GetDepartment_ShouldBeSuccess()
    {
        using var adminScope = _fixture.AdminScope();
        var testUser = _fixture.Fusion.CreateUser().AsEmployee().AzureUniqueId!.Value;

        var department = await _client.PutDepartmentAsync(testUser);
        var response = await _client.GetDepartmentAsync(department.DepartmentSapId);

        response.Should().BeSuccessfull();
    }


    [Fact]
    public async Task PutDepartment_Then_UpdateOwner_ShouldBeSuccess()
    {
        using var adminScope = _fixture.AdminScope();
        var oldOwner = _fixture.Fusion.CreateUser().AsEmployee().AzureUniqueId!.Value;


        var department = await _client.PutDepartmentAsync(oldOwner);

        var newOwner = _fixture.Fusion.CreateUser().AsEmployee();

        await _client.PutDepartmentAsync(newOwner.AzureUniqueId!.Value, d =>
        {
            d.DepartmentSapId = department.DepartmentSapId;
            d.FullDepartmentName = department.FullDepartmentName;
        });

        var updatedDepartment = (await _client.GetDepartmentAsync(department.DepartmentSapId)).Value!;

        updatedDepartment.DepartmentSapId.Should()
            .Be(department.DepartmentSapId, "It is still the same department with the same sap id");

        updatedDepartment.ResourceOwnerAzureUniqueId.Should()
            .Be(newOwner.AzureUniqueId!.Value, "The owner should be updated");

        updatedDepartment.FullDepartmentName.Should()
            .Be(department.FullDepartmentName, "It is still the same department with the same full department name");
    }
}