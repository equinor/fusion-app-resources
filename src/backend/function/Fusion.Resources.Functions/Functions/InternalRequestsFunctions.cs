#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Resources.Functions.ApiClients;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Functions;

public class InternalRequestsFunctions
{
    private readonly IResourcesApiClient resourcesClient;
    private readonly IPeopleApiClient peopleClient;
    private readonly ILineOrgApiClient lineOrgClient;
    private readonly ILogger<InternalRequestsFunctions> log;

    public InternalRequestsFunctions(IPeopleApiClient peopleClient, IResourcesApiClient resourcesClient, ILineOrgApiClient lineOrgClient, ILogger<InternalRequestsFunctions> logger)
    {
        this.resourcesClient = resourcesClient;
        this.lineOrgClient = lineOrgClient;
        this.peopleClient = peopleClient;

        this.log = logger;
    }

    [Singleton]
    [FunctionName("internal-requests-reassign-invalid-departments")]
    public async Task ReassignResourceAllocationRequestsWithInvalidDepartment([TimerTrigger("0 0 6 * * 0", RunOnStartup = false)] TimerInfo timer)
    {
        log.LogTrace($"Next occurrences: {timer.FormatNextOccurrences(3)}");

        var projects = await resourcesClient.GetProjectsAsync();
        var depList = await lineOrgClient.GetOrgUnitDepartmentsAsync();

        foreach (var project in projects)
        {
            log.LogDebug($"Processing project {project.Id}, fetching requests");

            var requestsByProject = await resourcesClient.GetIncompleteDepartmentAssignedResourceAllocationRequestsForProjectAsync(project);

            // Should only process requests not having a valid department
            foreach (var item in requestsByProject.Where(x => depList.Contains(x.AssignedDepartment) == false))
            {
                if (HasInvalidResourceAllocationRequestReferences(item))
                {
                    continue;
                }

                if (item.ProposedPerson?.Person.AzureUniquePersonId is not null)
                {
                    await ReAssignByProposedPersonAsync(item);
                }
                else
                {
                    await resourcesClient.ReassignRequestAsync(item, item.OrgPosition!.BasePosition.Department);
                }
            }
        }
    }

    /// <summary>
    /// Checks if request doesn't contain valid data for reassignment
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private bool HasInvalidResourceAllocationRequestReferences(IResourcesApiClient.ResourceAllocationRequest item)
    {
        if (item.OrgPosition is null)
        {
            log.LogError("Unable to resolve position used for request. Most likely deleted");
            return true;
        }
        if (item.OrgPositionInstance is null)
        {
            log.LogError("Unable to resolve position instance used for request. Most likely deleted");
            return true;
        }

        var basePositionDepartment = item.OrgPosition!.BasePosition.Department;
        if (basePositionDepartment is null)
        {
            log.LogError("Unable to resolve base position used for request. Most likely position is deleted");
            return true;
        }

        if (basePositionDepartment == item.AssignedDepartment)
        {
            log.LogDebug("Request OK. AssignedDepartment matches base position Department");
            return true;
        }

        return false;
    }

    private async Task ReAssignByProposedPersonAsync(IResourcesApiClient.ResourceAllocationRequest item)
    {
        string resolvedPersonFullDepartment;
        var proposedPersonAzureUniqueId = item.ProposedPerson?.Person.AzureUniquePersonId;
        if (proposedPersonAzureUniqueId is null)
        {
            return;
        }
        log.LogDebug("Request contains a proposed person");

        try
        {
            resolvedPersonFullDepartment = await peopleClient.GetPersonFullDepartmentAsync(proposedPersonAzureUniqueId);
        }
        catch (Exception ex)
        {
            log.LogError(ex.Message);
            return;
        }

        if (item.AssignedDepartment == resolvedPersonFullDepartment)
        {
            log.LogDebug("Request OK. AssignedDepartment matches proposed person FullDepartment");
            return;
        }

        if (resolvedPersonFullDepartment is null)
        {
            log.LogError("Request OK, but unable to resolve assigned person FullDepartment");
            return;
        }

        log.LogInformation("Request assigned incorrectly. AssignedDepartment is to be updated to match proposed person.");
        await resourcesClient.ReassignRequestAsync(item, resolvedPersonFullDepartment);
    }
}