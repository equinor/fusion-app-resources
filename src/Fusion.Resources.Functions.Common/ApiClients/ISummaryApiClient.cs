namespace Fusion.Resources.Functions.Common.ApiClients;

public interface ISummaryApiClient
{
    public Task PutDepartmentsAsync(IEnumerable<LineOrgApiClient.OrgUnits> orgUnits, CancellationToken cancellationToken = default);
}