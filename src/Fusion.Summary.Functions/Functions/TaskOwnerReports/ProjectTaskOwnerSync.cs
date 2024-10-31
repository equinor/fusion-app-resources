using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.Extensions;
using Fusion.Services.Org.ApiModels;
using Fusion.Summary.Functions.Functions.Helpers;
using Fusion.Summary.Functions.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fusion.Summary.Functions.Functions.TaskOwnerReports;

public class ProjectTaskOwnerSync
{
    private readonly ILogger<ProjectTaskOwnerSync> logger;
    private readonly IOrgClient orgClient;
    private readonly IRolesApiClient rolesApiClient;
    private readonly ISummaryApiClient summaryApiClient;

    // Configuration variables
    private string _serviceBusConnectionString;
    private string _weeklySummaryQueueName;
    private string[] _projectTypeFilter;
    private TimeSpan _totalBatchTime;

    public ProjectTaskOwnerSync(ILogger<ProjectTaskOwnerSync> logger, IConfiguration configuration, IOrgClient orgClient, IRolesApiClient rolesApiClient, ISummaryApiClient summaryApiClient)
    {
        this.logger = logger;
        this.orgClient = orgClient;
        this.rolesApiClient = rolesApiClient;
        this.summaryApiClient = summaryApiClient;

        _serviceBusConnectionString = configuration["AzureWebJobsServiceBus"]!;
        _weeklySummaryQueueName = configuration["project_summary_weekly_queue"]!;
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

    private const string FunctionName = "weekly-project-recipients-sync";

    [FunctionName(FunctionName)]
    public async Task RunAsync(
        [TimerTrigger("0 5 0 * * MON", RunOnStartup = false)]
        TimerInfo myTimer, CancellationToken cancellationToken)
    {
        await using var client = new ServiceBusClient(_serviceBusConnectionString);
        await using var sender = client.CreateSender(_weeklySummaryQueueName);

        logger.LogInformation("{FunctionName} triggered with projectTypeFilter {ProjectTypeFilter}", FunctionName, _projectTypeFilter.ToJson());

        #region Retrieve projects and admins

        var projects = await GetActiveOrgProjectsAsync(cancellationToken);
        var existingSummaryProjects = await summaryApiClient.GetProjectsAsync(cancellationToken);

        logger.LogInformation("Found {ProjectCount} active projects {Projects}", projects.Count, projects.Select(p => new { p.ProjectId, p.Name, p.DomainId }).ToJson());

        var projectAdminsMapping = await rolesApiClient.GetAdminRolesForOrgProjects(projects.Select(p => p.ProjectId), cancellationToken);

        var projectToEnqueueTimeMapping = QueueTimeHelper.CalculateEnqueueTime(projects, _totalBatchTime, logger);

        #endregion

        logger.LogInformation("Syncing projects and admins");

        foreach (var (orgProject, queueTime) in projectToEnqueueTimeMapping)
        {
            // Get recipients
            var projectAdmins = projectAdminsMapping.TryGetValue(orgProject.ProjectId, out var values)
                ? values.Where(p => p.Person is not null && p.Person.Id != Guid.Empty).ToArray()
                : [];

            var projectDirector = orgProject.Director.Instances
                .Where(i => i.Type == ApiPositionInstanceV2.ApiInstanceType.Normal.ToString() || i.Type == ApiPositionInstanceV2.ApiInstanceType.Rotation.ToString())
                .FirstOrDefault(i => i.AssignedPerson is not null && i.AppliesFrom <= DateTime.UtcNow && i.AppliesTo >= DateTime.UtcNow)?.AssignedPerson;

            // No point in storing project and creating a project if there are no recipients
            if (projectDirector is null && projectAdmins.Length == 0)
            {
                logger.LogInformation("Project {Name} ({ProjectId}) has no director or admins, skipping", orgProject.Name, orgProject.ProjectId);
                continue;
            }

            // OrgProjectExternalId is the common key between the two systems, org api and summary api
            // We use this to see if we're updating or creating a new project entity
            var existingProjectId = existingSummaryProjects.FirstOrDefault(p => p.OrgProjectExternalId == orgProject.ProjectId)?.Id;

            var apiProject = new ApiProject()
            {
                Id = existingProjectId ?? Guid.NewGuid(),
                OrgProjectExternalId = orgProject.ProjectId,
                Name = orgProject.Name,
                DirectorAzureUniqueId = projectDirector?.AzureUniqueId,
                AssignedAdminsAzureUniqueId = projectAdmins
                    .Select(p => p.Person!.Id)
                    .ToArray()
            };

            try
            {
                apiProject = await summaryApiClient.PutProjectAsync(apiProject, CancellationToken.None);
            }
            catch (SummaryApiError e)
            {
                logger.LogCritical(e, "Failed to PUT project {Project}", orgProject.ToJson());
                continue;
            }


            var message = new WeeklyTaskOwnerReportMessage()
            {
                ProjectId = apiProject.Id,
                OrgProjectExternalId = apiProject.OrgProjectExternalId,
                ProjectName = apiProject.Name,
                ProjectAdmins = projectAdmins
                    .Select(p => new WeeklyTaskOwnerReportMessage.ProjectAdmin()
                    {
                        AzureUniqueId = p.Person!.Id,
                        Mail = p.Person!.Mail,
                        ValidTo = p.ValidTo
                    })
                    .ToArray()
            };

            try
            {
                await SendProjectToQueue(sender, message, queueTime);
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Failed to send project to queue {Project}", message.ToJson());
            }
        }

        logger.LogInformation("{FunctionName} completed", FunctionName);
    }

    private async Task SendProjectToQueue(ServiceBusSender sender, WeeklyTaskOwnerReportMessage projectMessage, DateTimeOffset enqueueTime)
    {
        var serializedDto = JsonConvert.SerializeObject(projectMessage);

        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(serializedDto))
        {
            ScheduledEnqueueTime = enqueueTime
        };

        await sender.SendMessageAsync(message);
    }

    private async Task<List<ApiProjectV2>> GetActiveOrgProjectsAsync(CancellationToken cancellationToken)
    {
        var queryFilter = new ODataQuery();

        if (_projectTypeFilter.Length != 0)
        {
            queryFilter.Filter = $"projectType in ({string.Join(',', _projectTypeFilter.Select(s => $"'{s}'"))})";
        }

        const string stateFilter = "state in ('ACTIVE', 'null')";

        queryFilter.Filter = queryFilter.Filter is null ? stateFilter : $"{queryFilter.Filter} and {stateFilter}";

        var projects = await orgClient.GetProjectsAsync(queryFilter, cancellationToken);
        return projects;
    }
}