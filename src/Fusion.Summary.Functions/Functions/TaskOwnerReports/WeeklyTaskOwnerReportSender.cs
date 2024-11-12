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
    private AdaptiveCardRenderer cardHtmlRenderer;
    private readonly bool sendingNotificationEnabled = true; // Default to true so that we don't accidentally disable sending notifications

    public WeeklyTaskOwnerReportSender(ILogger<WeeklyTaskOwnerReportSender> logger, IConfiguration configuration, ISummaryApiClient summaryApiClient, IMailApiClient mailApiClient, IPeopleApiClient peopleApiClient)
    {
        this.logger = logger;
        this.summaryApiClient = summaryApiClient;
        this.mailApiClient = mailApiClient;
        this.peopleApiClient = peopleApiClient;
        cardHtmlRenderer = new AdaptiveCardRenderer();

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

        var projects = await GetProjectsAsync(cancellationToken);

        var taskOwnerReports = await GetTaskOwnerReportsAsync(projects, cancellationToken);

        var mailRequests = await CreateMailRequestsAsync(projects, taskOwnerReports, cancellationToken);

        await SendTaskOwnerReportsAsync(mailRequests);
    }

    public async Task<ICollection<ApiProject>> GetProjectsAsync(CancellationToken cancellationToken = default)
        => await summaryApiClient.GetProjectsAsync(cancellationToken);

    public async Task<ApiWeeklyTaskOwnerReport[]> GetTaskOwnerReportsAsync(IEnumerable<ApiProject> projects, CancellationToken cancellationToken = default)
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

    public async Task<List<SendEmailWithTemplateRequest>> CreateMailRequestsAsync(IEnumerable<ApiProject> projects, ICollection<ApiWeeklyTaskOwnerReport> taskOwnerReports, CancellationToken cancellationToken)
    {
        var requests = new List<SendEmailWithTemplateRequest>();

        // TODO: Recipients should be stored on the report itself, alternatively retried specifically from the summary api
        // For now we just extract the recipients from the api project model and resolve email addresses during the creation of the mail request

        foreach (var project in projects)
        {
            var report = taskOwnerReports.FirstOrDefault(r => r.ProjectId == project.Id);
            if (report is null)
                continue;

            var recipients = project.AssignedAdminsAzureUniqueId;
            if (project.DirectorAzureUniqueId.HasValue)
                recipients = recipients.Append(project.DirectorAzureUniqueId.Value).ToArray();

            if (recipients.Length == 0)
            {
                logger.LogWarning("No recipients found for project {Project}", project.ToJson());
                continue;
            }

            string[] recipientEmails;
            try
            {
                // TODO: Email resolution should be done before the az func sender runs, and the resolved emails should be stored on the report/project
                recipientEmails = await ResolveEmailsAsync(recipients, cancellationToken);
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


    private async Task<string[]> ResolveEmailsAsync(IEnumerable<Guid> azureUniqueId, CancellationToken cancellationToken)
    {
        var personIdentifiers = azureUniqueId.Select(id => new PersonIdentifier(id));

        var resolvedPersons = await peopleApiClient.ResolvePersonsAsync(personIdentifiers, cancellationToken);

        resolvedPersons.Where(p => !p.Success).ToList().ForEach(p => logger.LogWarning("Failed to resolve person {PersonId}", p.Identifier));

        return resolvedPersons
            .Where(p => p.Success)
            .Select(p => p.Person!.PreferredContactMail ?? p.Person.Mail).ToArray();
    }

    private SendEmailWithTemplateRequest CreateReportMail(string[] recipients, ApiProject project, ApiWeeklyTaskOwnerReport report)
    {
        var subject = $"Weekly summary - {project.Name}";

        var card = new AdaptiveCardBuilder()
            .AddHeading($"**Weekly summary - {project.Name}**")
            .AddTextRow(report.ActionsAwaitingTaskOwnerAction.ToString(), "Actions awaiting task owners")
            .AddListContainer("Admin access expire less than 3 months", report.AdminAccessExpiringInLessThanThreeMonths
                .Select(a => new List<AdaptiveCardBuilder.ListObject>()
                {
                    new()
                    {
                        Value = a.FullName,
                        Alignment = AdaptiveHorizontalAlignment.Left
                    },
                    new()
                    {
                        Value = a.Expires.ToString(),
                        Alignment = AdaptiveHorizontalAlignment.Right
                    }
                }).ToList())
            .AddTextRow(report.PositionAllocationsEndingInNextThreeMonths.Length.ToString(), "Position allocations expiring next 3 months")
            .AddTextRow(report.TBNPositionsStartingInLessThanThreeMonths.Length.ToString(), "TBN positions with start date in less than 3 months")
            .Build();


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

    public async Task SendTaskOwnerReportsAsync(IEnumerable<SendEmailWithTemplateRequest> emailReportRequests)
    {
        foreach (var request in emailReportRequests)
        {
            try
            {
                if (sendingNotificationEnabled)
                    await mailApiClient.SendEmailWithTemplate(request);
                else
                    logger.LogInformation("Sending of notifications is disabled. Skipping sending mail to {Recipients}", string.Join(',', request.Recipients));
            }
            catch (ApiError e)
            {
                logger.LogError(e, "Failed to send task owner report mail. Request: {Request}", request.ToJson());
            }
        }
    }
}