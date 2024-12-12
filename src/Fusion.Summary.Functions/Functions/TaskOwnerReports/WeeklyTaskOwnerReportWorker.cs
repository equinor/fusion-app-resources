using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.Extensions;
using Fusion.Summary.Functions.Models;
using Fusion.Summary.Functions.ReportCreator;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using PersonIdentifier = Fusion.Integration.Profile.PersonIdentifier;

namespace Fusion.Summary.Functions.Functions.TaskOwnerReports;

public class WeeklyTaskOwnerReportWorker
{
    private readonly ISummaryApiClient summaryApiClient;
    private readonly IResourcesApiClient resourcesApiClient;
    private readonly IPeopleApiClient peopleApiClient;
    private readonly IOrgClient orgApiClient;
    private readonly ILogger<WeeklyTaskOwnerReportWorker> logger;

    public WeeklyTaskOwnerReportWorker(ISummaryApiClient summaryApiClient, ILogger<WeeklyTaskOwnerReportWorker> logger, IResourcesApiClient resourcesApiClient, IPeopleApiClient peopleApiClient, IOrgClient orgApiClient)
    {
        this.summaryApiClient = summaryApiClient;
        this.logger = logger;
        this.resourcesApiClient = resourcesApiClient;
        this.peopleApiClient = peopleApiClient;
        this.orgApiClient = orgApiClient;
    }

    private const string FunctionName = "weekly-task-owner-report-worker";

    [FunctionName(FunctionName)]
    public async Task RunAsync(
        [ServiceBusTrigger("%project_summary_weekly_queue%", Connection = "AzureWebJobsServiceBus")]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageReceiver, CancellationToken cancellationToken)
    {
        var dto = await JsonSerializer.DeserializeAsync<WeeklyTaskOwnerReportMessage>(message.Body.ToStream(), cancellationToken: cancellationToken);

        logger.LogInformation("{FunctionName} started with message: {MessageBody}", FunctionName, dto.ToJson());
        try
        {
            await CreateAndStoreReportAsync(dto, cancellationToken);
            await messageReceiver.CompleteMessageAsync(message, cancellationToken);
            logger.LogInformation($"{FunctionName} completed successfully");
        }
        catch (Exception e) // Dead letter message
        {
            logger.LogError(e, $"{FunctionName} completed with error");
            throw;
        }
    }

    private async Task CreateAndStoreReportAsync(WeeklyTaskOwnerReportMessage message, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow.Date;
        WeeklyTaskOwnerReportDataCreator.NowDate = now;
        // Exclude Products
        var allProjectPositions = (await orgApiClient.GetProjectPositions(message.OrgProjectExternalId.ToString(), cancellationToken))
            .Where(p => p.BasePosition.ProjectType != "Product").ToArray();
        var activeRequestsForProject = await resourcesApiClient.GetActiveRequestsForProjectAsync(message.OrgProjectExternalId, cancellationToken);
        var admins = await ResolveAdminsAsync(message, cancellationToken);

        var expiringAdmins = WeeklyTaskOwnerReportDataCreator.GetExpiringAdmins(admins);
        var actionsAwaitingTaskOwner = WeeklyTaskOwnerReportDataCreator.GetActionsAwaitingTaskOwnerAsync(activeRequestsForProject);
        var expiringPositionAllocations = WeeklyTaskOwnerReportDataCreator.GetPositionAllocationsEndingNextThreeMonths(allProjectPositions);
        var tbnPositions = WeeklyTaskOwnerReportDataCreator.GetTBNPositionsStartingWithinThreeMonths(allProjectPositions);

        var lastMonday = now.GetPreviousWeeksMondayDate();
        var report = new ApiWeeklyTaskOwnerReport()
        {
            Id = Guid.Empty,
            PeriodStart = lastMonday,
            PeriodEnd = lastMonday.AddDays(7),
            ProjectId = message.ProjectId,
            ActionsAwaitingTaskOwnerAction = actionsAwaitingTaskOwner,
            AdminAccessExpiringInLessThanThreeMonths = expiringAdmins.Select(ea => new ApiAdminAccessExpiring()
            {
                AzureUniqueId = ea.AzureUniqueId,
                FullName = ea.FullName,
                Expires = ea.ValidTo
            }).ToArray(),
            PositionAllocationsEndingInNextThreeMonths = expiringPositionAllocations.Select(ep => new ApiPositionAllocationEnding()
            {
                PositionName = ep.Position.BasePosition.Name ?? string.Empty,
                PositionNameDetailed = ep.Position.Name,
                PositionExternalId = ep.Position.ExternalId ?? string.Empty,
                PositionAppliesTo = ep.ExpiresAt
            }).ToArray(),
            TBNPositionsStartingInLessThanThreeMonths = tbnPositions.Select(tp => new ApiTBNPositionStartingSoon()
            {
                PositionName = tp.Position.BasePosition.Name ?? string.Empty,
                PositionNameDetailed = tp.Position.Name,
                PositionExternalId = tp.Position.ExternalId ?? string.Empty,
                PositionAppliesFrom = tp.StartsAt
            }).ToArray()
        };


        await summaryApiClient.PutWeeklyTaskOwnerReportAsync(message.ProjectId, report, cancellationToken);
    }


    private async Task<List<PersonAdmin>> ResolveAdminsAsync(WeeklyTaskOwnerReportMessage message, CancellationToken cancellationToken)
    {
        if (message.ProjectAdmins.Length == 0)
            return [];

        var personIdentifiers = message.ProjectAdmins.Select(pa => new PersonIdentifier(pa.AzureUniqueId, pa.Mail));

        var resolvedAdmins = await peopleApiClient.ResolvePersonsAsync(personIdentifiers, cancellationToken);

        var admins = new List<PersonAdmin>();

        foreach (var resolvedPersonProfile in resolvedAdmins)
        {
            if (!resolvedPersonProfile.Success)
            {
                logger.LogWarning("Failed to resolve profile for {PersonIdentifier}", resolvedPersonProfile.Identifier);
                continue;
            }

            var profile = resolvedPersonProfile.Person!;

            if (profile.AzureUniqueId == null)
            {
                logger.LogError("Resolved profile for {PersonIdentifier} does not have AzureUniqueId", resolvedPersonProfile.Identifier);
                continue;
            }

            var projectAdmin = message.ProjectAdmins.First(pa => pa.AzureUniqueId == profile.AzureUniqueId ||
                                                                 pa.Mail != null && pa.Mail.Equals(profile.Mail, StringComparison.OrdinalIgnoreCase));

            if (projectAdmin.ValidTo == null)
                continue;

            admins.Add(new PersonAdmin(profile.AzureUniqueId.Value, profile.Name, projectAdmin.ValidTo.Value.DateTime));
        }


        return admins;
    }
}