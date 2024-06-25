namespace Fusion.Resources.Functions.Common.ApiClients;

public class SummaryApiClient : ISummaryApiClient
{
    public Task PutDepartmentsAsync(IEnumerable<LineOrgApiClient.OrgUnits> orgUnits, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}