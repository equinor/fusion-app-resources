using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Testing;

namespace Fusion.Summary.Api.Tests.Helpers;

public static class DepartmentHelpers
{
    public static ApiDepartment GenerateDepartment() =>
        new()
        {
            FullDepartmentName = $"Test FullDepartmentName {Guid.NewGuid()}",
            DepartmentSapId = $"Test DepartmentSapId {Guid.NewGuid()}",
            ResourceOwnerAzureUniqueId = Guid.NewGuid()
        };

    public static async Task<ApiDepartment> PutDepartmentAsync(this HttpClient client,
        Action<ApiDepartment>? setup = null)
    {
        var department = GenerateDepartment();
        setup?.Invoke(department);

        var response = await client.TestClientPutAsync<object>($"departments/{department.DepartmentSapId}", department);
        response.Should().BeSuccessfull();
        return department;
    }

    public static async Task<TestClientHttpResponse<ApiDepartment>> GetDepartmentAsync(this HttpClient client,
        string departmentSapId)
    {
        var response = await client.TestClientGetAsync<ApiDepartment>($"departments/{departmentSapId}");
        return response;
    }
}