namespace Fusion.Resources.Functions.Common.ApiClients;

public interface ISummaryApiClient
{
    public Task PutDepartmentsAsync(IEnumerable<ApiResourceOwnerDepartments> departments,
        CancellationToken cancellationToken = default);

    public Task<ICollection<ApiResourceOwnerDepartments>> GetDepartmentsAsync(
        CancellationToken cancellationToken = default);

    public Task<ApiSummaryReport?> GetLatestWeeklyReportAsync(string departmentSapId,
        CancellationToken cancellationToken = default);
}

#region Models

public class ApiResourceOwnerDepartments
{
    public ApiResourceOwnerDepartments(string departmentSapId, string fullDepartmentName,
        Guid resourceOwnerAzureUniqueId)
    {
        DepartmentSapId = departmentSapId;
        FullDepartmentName = fullDepartmentName;
        ResourceOwnerAzureUniqueId = resourceOwnerAzureUniqueId;
    }

    public string DepartmentSapId { get; init; }
    public string FullDepartmentName { get; init; }
    public Guid ResourceOwnerAzureUniqueId { get; init; }
}

public class ApiSummaryReport
{
}

#endregion