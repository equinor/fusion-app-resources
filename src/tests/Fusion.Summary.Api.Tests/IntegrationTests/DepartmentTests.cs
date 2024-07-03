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
    public async Task PutDepartment_Then_GetDepartment_ShouldBeSuccess()
    {
        using var adminScope = _fixture.AdminScope();
        var department = await _client.PutDepartmentAsync();

        var response = await _client.GetDepartmentAsync(department.DepartmentSapId);
        response.Should().BeSuccessfull();
    }


    [Fact]
    public async Task PutDepartment_Then_UpdateOwner_ShouldBeSuccess()
    {
        using var adminScope = _fixture.AdminScope();

        var department = await _client.PutDepartmentAsync();

        var newOwner = _fixture.Fusion.CreateUser().AsEmployee();

        await _client.PutDepartmentAsync(d =>
        {
            d.ResourceOwnerAzureUniqueId = newOwner.AzureUniqueId!.Value;
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