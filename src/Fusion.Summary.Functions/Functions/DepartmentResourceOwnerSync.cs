using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
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
        [TimerTrigger("0 05 00 * * *", RunOnStartup = false)]
        TimerInfo timerInfo, CancellationToken cancellationToken
    )
    {
        var departments = await lineOrgApiClient.GetOrgUnitDepartmentsAsync();

        var selectedDepartments = departments
            .Where(d => d.FullDepartment != null).DistinctBy(d => d.SapId).ToList();

        if (!selectedDepartments.Any())
            throw new Exception("No departments found.");

        // TODO: Retrieving resource-owners wil be refactored later
        var resourceOwners = new List<LineOrgPerson>();
        foreach (var orgUnitsChunk in selectedDepartments.Chunk(10))
        {
            var chunkedResourceOwners =
                await lineOrgApiClient.GetResourceOwnersFromFullDepartment(orgUnitsChunk);
            resourceOwners.AddRange(chunkedResourceOwners);
        }

        if (!resourceOwners.Any())
            throw new Exception("No resource-owners found.");

        var resourceOwnerDepartments = resourceOwners
            .Where(ro => ro.DepartmentSapId is not null && Guid.TryParse(ro.AzureUniqueId, out _))
            .Select(resourceOwner => new
                ApiResourceOwnerDepartment(resourceOwner.DepartmentSapId!, resourceOwner.FullDepartment,
                    Guid.Parse(resourceOwner.AzureUniqueId)));


        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 10,
        };

        // Use Parallel.ForEachAsync to easily limit the number of parallel requests
        await Parallel.ForEachAsync(resourceOwnerDepartments, parallelOptions,
            async (ownerDepartment, token) =>
            {
                await summaryApiClient.PutDepartmentAsync(ownerDepartment, token);
            });
    }
}