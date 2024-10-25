using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.Extensions;
using Fusion.Summary.Functions.Functions.Helpers;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fusion.Summary.Functions.Functions.TaskOwnerReports;

public class ProjectTaskOwnerSync
{
    private const string FunctionName = "weekly-project-recipients-sync";
    private readonly ILogger<ProjectTaskOwnerSync> logger;
    private readonly IOrgClient orgClient;
    private readonly IRolesApiClient rolesApiClient;
    private readonly ISummaryApiClient summaryApiClient;

    private string _serviceBusConnectionString;
    private string _weeklySummaryQueueName;
    private string[]? _projectTypeFilter;
    private TimeSpan _totalBatchTime;

    public ProjectTaskOwnerSync(ILogger<ProjectTaskOwnerSync> logger, IConfiguration configuration, IOrgClient orgClient, IRolesApiClient rolesApiClient, ISummaryApiClient summaryApiClient)
    {
        this.logger = logger;
        this.orgClient = orgClient;
        this.rolesApiClient = rolesApiClient;
        this.summaryApiClient = summaryApiClient;

        _serviceBusConnectionString = configuration["AzureWebJobsServiceBus"]!;
        _weeklySummaryQueueName = configuration["project_summary_weekly_queue"]!;
        // TODO: Should there be a default value for projectTypeFilter?
        _projectTypeFilter = configuration["projectTypeFilter"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? ["PRD"];

        var totalBatchTimeInMinutesStr = configuration["total_batch_time_in_minutes"];

        if (!string.IsNullOrWhiteSpace(totalBatchTimeInMinutesStr))
        {
            _totalBatchTime = TimeSpan.FromMinutes(double.Parse(totalBatchTimeInMinutesStr));
            this.logger.LogInformation("Batching messages over {BatchTime}", _totalBatchTime);
        }
        else
        {
            _totalBatchTime = TimeSpan.FromHours(4.5);

            this.logger.LogInformation("Configuration variable 'total_batch_time_in_minutes' not found, batching messages over {BatchTime}", _totalBatchTime);
        }
    }


    [FunctionName(FunctionName)]
    public async Task RunAsync(
        [TimerTrigger("0 5 0 * * MON", RunOnStartup = false)]
        TimerInfo myTimer)
    {
        // TODO: Should gather all relevant projects

        logger.LogInformation("{FunctionName} triggered with projectTypeFilter {ProjectTypeFilter}", FunctionName, _projectTypeFilter?.ToJson());

        var queryFilter = new ODataQuery();

        if (_projectTypeFilter != null)
        {
            queryFilter.Filter = $"projectType in ({string.Join(',', _projectTypeFilter)})";
            // TODO: Filter on active projects when odata support is added in org
            // + " and state eq 'ACTIVE'";
        }

        // TODO: Should projects be retrieved from context api?
        // Can we used project id from the org api as identifier?
        // Context api does not have project type
        var projects = await orgClient.GetProjects(queryFilter);

        var activeProjects = projects.Where(p => p.State.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase)).ToList();

        if (_projectTypeFilter is not null)
            activeProjects = activeProjects.Where(p => _projectTypeFilter.Contains(p.ProjectType, StringComparer.OrdinalIgnoreCase)).ToList();

        logger.LogInformation("Found {ProjectCount} active projects {Projects}", activeProjects.Count, activeProjects.Select(p => new { p.ProjectId, p.Name, p.DomainId }).ToJson());
        var admins = await rolesApiClient.GetAdminRolesForOrgProjects(activeProjects.Select(p => p.ProjectId));


        var projectToEnqueueTime = QueueTimeHelper.CalculateEnqueueTime(activeProjects, _totalBatchTime, logger);

        logger.LogInformation("Syncing projects and admins");

        foreach (var (project, queueTime) in projectToEnqueueTime)
        {
            try
            {
                var projectAdmins = admins.TryGetValue(project.ProjectId, out var values) ? values : [];
                var projectDirector = project.Director.Instances
                    .FirstOrDefault(i => i.AssignedPerson is not null && i.AppliesFrom <= DateTime.UtcNow && i.AppliesTo >= DateTime.UtcNow)?.AssignedPerson;

                var putRequest = new ApiProject()
                {
                    Id = Guid.Empty, // Ignored
                    OrgProjectExternalId = project.ProjectId,
                    Name = project.Name,
                    DirectorAzureUniqueId = projectDirector?.AzureUniqueId,
                    AssignedAdminsAzureUniqueId = projectAdmins
                        .Where(p => p.Person?.AzureUniqueId is not null)
                        .Select(p => p.Person!.AzureUniqueId)
                        .ToArray()
                };

                await summaryApiClient.PutProjectAsync(putRequest);
            }
            catch (SummaryApiError e)
            {
                logger.LogCritical(e, "Failed to PUT project {Project}", project.ToJson());
                continue;
            }

            // TODO: Should send project and recipients on to the bus
        }
    }
}