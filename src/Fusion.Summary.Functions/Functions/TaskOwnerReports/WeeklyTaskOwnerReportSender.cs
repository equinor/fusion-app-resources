using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using AdaptiveCards.Rendering.Html;
using Fusion.Integration.Profile;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using Fusion.Resources.Functions.Common.Extensions;
using Fusion.Resources.Functions.Common.Integration.Errors;
using Fusion.Summary.Functions.CardBuilder;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fusion.Summary.Functions.Functions.TaskOwnerReports;

public class WeeklyTaskOwnerReportSender
{
    private readonly ILogger<WeeklyTaskOwnerReportSender> logger;
    private readonly ISummaryApiClient summaryApiClient;
    private readonly IMailApiClient mailApiClient;
    private readonly IPeopleApiClient peopleApiClient;
    private readonly IContextApiClient contextApiClient;
    private readonly AdaptiveCardRenderer cardHtmlRenderer;
    private readonly bool sendingNotificationEnabled = true; // Default to true so that we don't accidentally disable sending notifications
    private readonly string fusionUri;

    public WeeklyTaskOwnerReportSender(ILogger<WeeklyTaskOwnerReportSender> logger, IConfiguration configuration, ISummaryApiClient summaryApiClient, IMailApiClient mailApiClient, IPeopleApiClient peopleApiClient, IContextApiClient contextApiClient)
    {
        this.logger = logger;
        this.summaryApiClient = summaryApiClient;
        this.mailApiClient = mailApiClient;
        this.peopleApiClient = peopleApiClient;
        this.contextApiClient = contextApiClient;
        cardHtmlRenderer = new AdaptiveCardRenderer();
        fusionUri = (configuration["Endpoints_portal"] ?? "https://fusion.equinor.com/").TrimEnd('/');

        // Need to explicitly add the configuration key to the app settings to disable sending of notifications
        if (int.TryParse(configuration["isSendingNotificationEnabled"], out var enabled))
            sendingNotificationEnabled = enabled == 1;
        else if (bool.TryParse(configuration["isSendingNotificationEnabled"], out var enabledBool))
            sendingNotificationEnabled = enabledBool;
    }

    private const string FunctionName = "weekly-task-owner-report-sender";

    [FunctionName(FunctionName)]
    public async Task RunAsync([TimerTrigger("0 0 5 * * MON", RunOnStartup = false)] TimerInfo timerInfo, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{FunctionName} started", FunctionName);

        if (!sendingNotificationEnabled)
            logger.LogInformation("Sending of notifications is disabled");

        var projects = await GetProjectsInformationAsync(cancellationToken);

        var taskOwnerReports = await GetTaskOwnerReportsAsync(projects, cancellationToken);

        var mailRequests = await CreateMailRequestsAsync(projects, taskOwnerReports, cancellationToken);

        await SendTaskOwnerReportsAsync(mailRequests);
    }

    private async Task<List<Project>> GetProjectsInformationAsync(CancellationToken cancellationToken = default)
    {
        var apiProjects = await summaryApiClient.GetProjectsAsync(cancellationToken);

        ICollection<ApiContext> apiOrgContexts;
        try
        {
            apiOrgContexts = await contextApiClient.GetContextsAsync("OrgChart", cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            // Log and continue
            // Everything else works but the "Go To" links in the mail will not work
            logger.LogError(e, "Failed to get org contexts");
            apiOrgContexts = [];
        }

        var mergedProjects = new List<Project>();

        foreach (var project in apiProjects)
        {
            // TODO: Recipients should be stored on the report itself, alternatively retried specifically from the summary api
            // For now we just extract the recipients from the api project model and resolve email addresses during the creation of the mail request

            var recipients = project.AssignedAdminsAzureUniqueId;
            if (project.DirectorAzureUniqueId.HasValue)
                recipients = recipients.Append(project.DirectorAzureUniqueId.Value).ToArray();

            if (recipients.Length == 0)
            {
                logger.LogWarning("No recipients found for project {Project}", project.ToJson());
                continue;
            }

            recipients = recipients.Distinct().ToArray();

            // For contexts of type OrgChart
            // The ExternalId is the same as the internal id used for a project in the Org API
            // The value property bag also contains this externalId. value.orgChartId

            // So we try and find the common external id between the project and the org context

            var orgProjectContext = apiOrgContexts.FirstOrDefault(c => Guid.TryParse(c.ExternalId, out var contextExternalId)
                                                                       && contextExternalId == project.OrgProjectExternalId);


            if (orgProjectContext is null) // Try and check the PropertyBag
                orgProjectContext = apiOrgContexts.FirstOrDefault(c => c.Value.TryGetValue("orgChartId", out var orgChartId)
                                                                       && Guid.TryParse(orgChartId as string, out var contextExternalId)
                                                                       && contextExternalId == project.OrgProjectExternalId);


            if (orgProjectContext is null)
                logger.LogError("No org context found for project {Project}", project.ToJson());


            mergedProjects.Add(new Project()
            {
                Id = project.Id,
                OrgProjectExternalId = project.OrgProjectExternalId,
                ContextProjectId = orgProjectContext?.Id,
                Name = project.Name,
                Recipients = recipients
            });
        }

        return mergedProjects;
    }

    private async Task<ApiWeeklyTaskOwnerReport[]> GetTaskOwnerReportsAsync(IEnumerable<Project> projects, CancellationToken cancellationToken = default)
    {
        var taskOwnerReports = new List<ApiWeeklyTaskOwnerReport>();
        foreach (var project in projects)
        {
            try
            {
                var report = await summaryApiClient.GetLatestWeeklyTaskOwnerReportAsync(project.Id, cancellationToken);

                if (report is null)
                    continue;

                taskOwnerReports.Add(report);
            }
            catch (SummaryApiError e)
            {
                logger.LogError(e, "Failed to get task owner report for project {Project}", project.ToJson());
            }
        }

        return taskOwnerReports.ToArray();
    }

    private async Task<List<SendEmailWithTemplateRequest>> CreateMailRequestsAsync(IEnumerable<Project> projects, ICollection<ApiWeeklyTaskOwnerReport> taskOwnerReports, CancellationToken cancellationToken)
    {
        var requests = new List<SendEmailWithTemplateRequest>();

        foreach (var project in projects)
        {
            var report = taskOwnerReports.FirstOrDefault(r => r.ProjectId == project.Id);
            if (report is null)
                continue;

            string[] recipientEmails;
            try
            {
                // TODO: Email resolution should be done before the az func sender runs, and the resolved emails should be stored on the report/project
                recipientEmails = await ResolveEmailsAsync(project.Recipients, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to resolve emails for project {Project} | Report {Report}", project.ToJson(), report.ToJson());
                continue;
            }


            try
            {
                requests.Add(CreateReportMail(recipientEmails, project, report));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to create mail request for project {Project} | Report {Report}", project.ToJson(), report.ToJson());
            }
        }

        return requests;
    }

    private async Task SendTaskOwnerReportsAsync(IEnumerable<SendEmailWithTemplateRequest> emailReportRequests)
    {
        foreach (var request in emailReportRequests)
        {
            try
            {
                if (sendingNotificationEnabled)
                    await mailApiClient.SendEmailWithTemplateAsync(request);
                else
                    logger.LogInformation("Sending of notifications is disabled. Skipping sending mail to {Recipients}", string.Join(',', request.Recipients));
            }
            catch (ApiError e)
            {
                logger.LogError(e, "Failed to send task owner report mail. Request: {Request}", request.ToJson());
            }
        }
    }


    private async Task<string[]> ResolveEmailsAsync(IEnumerable<Guid> azureUniqueId, CancellationToken cancellationToken)
    {
        var personIdentifiers = azureUniqueId.Select(id => new PersonIdentifier(id));

        var resolvedPersons = await peopleApiClient.ResolvePersonsAsync(personIdentifiers, cancellationToken);

        resolvedPersons.Where(p => !p.Success).ToList().ForEach(p => logger.LogWarning("Failed to resolve person {PersonId}", p.Identifier));

        return resolvedPersons
            .Where(p => p.Success)
            .Select(p => p.Person!.PreferredContactMail ?? p.Person.Mail).ToArray();
    }

    private SendEmailWithTemplateRequest CreateReportMail(string[] recipients, Project project, ApiWeeklyTaskOwnerReport report)
    {
        var contextId = project.ContextProjectId;

        var card = new AdaptiveCardBuilder()
            .AddHeading($"**Weekly summary - {project.Name}**")
            .AddTextRow(report.ActionsAwaitingTaskOwnerAction.ToString(), "Actions awaiting task owners")
            .AddGrid("Admin access expiring in less than 3 months", new List<GridColumn>()
            {
                new()
                {
                    Width = AdaptiveColumnWidth.Stretch,
                    Cells =
                    [
                        new GridCell(isHeader: true, value: "Name"),
                        ..report.AdminAccessExpiringInLessThanThreeMonths.Select(a
                            => new GridCell(isHeader: false, value: a.FullName))
                    ]
                },
                new()
                {
                    Width = AdaptiveColumnWidth.Auto,
                    Cells =
                    [
                        new GridCell(isHeader: true, value: "Expires"),
                        ..report.AdminAccessExpiringInLessThanThreeMonths.Select(a
                            => new GridCell(isHeader: false, value: a.Expires.ToString("dd/MM/yyyy")))
                    ]
                }
            }, new GoToAction()
            {
                Title = "Go to Access Management",
                Url = $"{fusionUri}/apps/org-admin/{contextId}/access-control"
            })
            .AddGrid("Position allocations expiring next 3 months", new List<GridColumn>()
            {
                new()
                {
                    Width = AdaptiveColumnWidth.Stretch,
                    Cells =
                    [
                        new GridCell(isHeader: true, value: "Position"),
                        ..report.PositionAllocationsEndingInNextThreeMonths.Select(p
                            => new GridCell(isHeader: false, value: $"{p.PositionExternalId} {p.PositionNameDetailed}"))
                    ]
                },
                new()
                {
                    Width = AdaptiveColumnWidth.Auto,
                    Cells =
                    [
                        new GridCell(isHeader: true, value: "End date"),
                        ..report.PositionAllocationsEndingInNextThreeMonths.Select(p
                            => new GridCell(isHeader: false, value: p.PositionAppliesTo.ToString("dd/MM/yyyy")))
                    ]
                }
            }, new GoToAction()
            {
                Title = "Go to Position Overview",
                Url = $"{fusionUri}/apps/org-admin/{contextId}/edit-positions/listing-view"
            })
            .AddGrid("TBN positions with start date in less than 3 months", new List<GridColumn>()
            {
                new()
                {
                    Width = AdaptiveColumnWidth.Stretch,
                    Cells =
                    [
                        new GridCell(isHeader: true, value: "Position"),
                        ..report.TBNPositionsStartingInLessThanThreeMonths.Select(p
                            => new GridCell(isHeader: false, value: $"{p.PositionExternalId} {p.PositionNameDetailed}"))
                    ]
                },
                new()
                {
                    Width = AdaptiveColumnWidth.Auto,
                    Cells =
                    [
                        new GridCell(isHeader: true, value: "Start date"),
                        ..report.TBNPositionsStartingInLessThanThreeMonths.Select(p
                            => new GridCell(isHeader: false, value: p.PositionAppliesFrom.ToString("dd/MM/yyyy")))
                    ]
                }
            }, new GoToAction()
            {
                Title = "Go to Position Overview",
                Url = $"{fusionUri}/apps/org-admin/{contextId}/edit-positions/listing-view"
            })
            .Build();

        var subject = $"Weekly summary - {project.Name}";

        return new SendEmailWithTemplateRequest()
        {
            Recipients = recipients,
            Subject = subject,
            MailBody = new()
            {
                HtmlContent = cardHtmlRenderer.RenderCard(card).Html.ToString()
            }
        };
    }


    private class Project
    {
        /// Internal id used in summary api
        public Guid Id { get; set; }

        // Internal id used in org Api
        public Guid OrgProjectExternalId { get; set; }

        /// Internal id of the connected org context. Used to create the url to the project in Fusion
        public Guid? ContextProjectId { get; set; }

        public string Name { get; set; } = string.Empty;

        public Guid[] Recipients { get; set; } = [];
    }
}