using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Functions.Common.ApiClients;
using Microsoft.Azure.WebJobs;

namespace Fusion.Summary.Functions.Functions;

public class DepartmentResourceOwnerSync
{
    private readonly ILineOrgApiClient lineOrgApiClient;
    private readonly ISummaryApiClient summaryApiClient;

    public DepartmentResourceOwnerSync(ILineOrgApiClient lineOrgApiClient, ISummaryApiClient summaryApiClient)
    {
        this.lineOrgApiClient = lineOrgApiClient;
        this.summaryApiClient = summaryApiClient;
    }

    [FunctionName("department-resource-owner-sync")]
    public async Task RunAsync(
        [TimerTrigger("0 05 00 * * *", RunOnStartup = false)] TimerInfo timerInfo, CancellationToken cancellationToken
    )
    {
        var orgUnits = await lineOrgApiClient.GetOrgUnitDepartmentsAsync();
        await summaryApiClient.PutDepartmentsAsync(orgUnits, cancellationToken);
    }
}