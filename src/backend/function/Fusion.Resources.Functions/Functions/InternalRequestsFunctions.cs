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

        var activeProjects = await resourcesClient.GetProjectsAsync();
        var activeDepartments = (await lineOrgClient.GetOrgUnitDepartmentsAsync()).ToList();

        foreach (var project in activeProjects)
        {
            log.LogDebug($"Processing project {project.Id}, fetching requests");

            var requestsByProject = await resourcesClient.GetIncompleteDepartmentAssignedResourceAllocationRequestsForProjectAsync(project);

            // Should only process requests not having a valid department
            foreach (var item in requestsByProject.Where(x => activeDepartments.Contains(x.AssignedDepartment) == false && IsValidRequest(x)))
            {
                if (item.HasProposedPerson)
                {
                    await ReAssignByProposedPersonAsync(item);
                }
                else
                {
                    var department =
                        activeDepartments.FirstOrDefault(x => string.Equals(x, item.OrgPosition!.BasePosition.Department, StringComparison.OrdinalIgnoreCase));

                    // If department is not found in existing departments, clear department and make request available for manual pickup in UI.
                    // This due to possible misconfiguration in base position departments.
                    await resourcesClient.ReassignRequestAsync(item, department);
                }
            }
        }
    }

    /// <summary>
    /// Checks if request doesn't contain valid data for reassignment
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private bool IsValidRequest(IResourcesApiClient.ResourceAllocationRequest item)
    {
        if (item.OrgPosition is null)
        {
            log.LogError("Unable to resolve position used for request. Most likely deleted");
            return false;
        }
        if (item.OrgPositionInstance is null)
        {
            log.LogError("Unable to resolve position instance used for request. Most likely deleted");
            return false;
        }

        var basePositionDepartment = item.OrgPosition!.BasePosition.Department;
        if (basePositionDepartment is null)
        {
            log.LogError("Unable to resolve base position used for request. Most likely position is deleted");
            return false;
        }

        if (string.Equals(basePositionDepartment, item.AssignedDepartment, StringComparison.OrdinalIgnoreCase))
            return false;

        // Request has valid connection to position and instance, but department mismatch compared to base position department.
        return true;
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