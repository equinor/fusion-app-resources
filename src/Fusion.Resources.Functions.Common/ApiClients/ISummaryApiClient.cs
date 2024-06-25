namespace Fusion.Resources.Functions.Common.ApiClients;

public interface ISummaryApiClient
{
    public Task PutDepartmentsAsync(IEnumerable<PutDepartmentRequest> departments, CancellationToken cancellationToken = default);
}