using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.Extensions;
using Fusion.Summary.Functions.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Fusion.Summary.Functions.Functions.TaskOwnerReports;

public class WeeklyTaskOwnerReportWorker
{
    private readonly ISummaryApiClient summaryApiClient;
    private readonly IFusionProfileResolver profileResolver;
    private readonly ILogger<WeeklyTaskOwnerReportWorker> logger;

    public WeeklyTaskOwnerReportWorker(ISummaryApiClient summaryApiClient, ILogger<WeeklyTaskOwnerReportWorker> logger, IFusionProfileResolver profileResolver)
    {
        this.summaryApiClient = summaryApiClient;
        this.logger = logger;
        this.profileResolver = profileResolver;
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
        // TODO: Remove
        /*
    Admin access expire less than 3 months (user can update access duration)
    Actions awaiting task owner (in project/task) (link to relevant view)
    Position allocations expiring next 3 months (link to relevant view)
    TBN positions with start date > 3 months (user should send request or update start date - link to relevant view)
         */

        var expiringAdmins = await GetExpiringAdminsAsync(message, cancellationToken);
        var actionsAwaitingTaskOwner = await GetActionsAwaitingTaskOwnerAsync(message, cancellationToken);
    }

    private async Task<int> GetActionsAwaitingTaskOwnerAsync(WeeklyTaskOwnerReportMessage message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<List<ExpiringAdmin>> GetExpiringAdminsAsync(WeeklyTaskOwnerReportMessage message, CancellationToken cancellationToken)
    {
        var personIdentifiers = message.ProjectAdmins.Select(pa => new PersonIdentifier(pa.AzureUniqueId, pa.Mail));

        var resolvedAdmins = await profileResolver.ResolvePersonsAsync(personIdentifiers, cancellationToken);

        var expiringAdmins = new List<ExpiringAdmin>();

        foreach (var resolvedPersonProfile in resolvedAdmins)
        {
            if (!resolvedPersonProfile.Success)
            {
                logger.LogWarning("Failed to resolve profile for {PersonIdentifier}", resolvedPersonProfile.Identifier);
                continue;
            }

            var profile = resolvedPersonProfile.Profile!;

            if (profile.AzureUniqueId == null)
            {
                logger.LogError("Resolved profile for {PersonIdentifier} does not have AzureUniqueId", resolvedPersonProfile.Identifier);
                continue;
            }

            var projectAdmin = message.ProjectAdmins.First(pa => pa.AzureUniqueId == profile.AzureUniqueId);

            if (projectAdmin.ValidTo == null)
                continue;

            if (projectAdmin.ValidTo.Value.Date <= DateTimeOffset.UtcNow.AddMonths(3).Date)
                expiringAdmins.Add(new ExpiringAdmin(profile.AzureUniqueId.Value, profile.Name, projectAdmin.ValidTo.Value));
        }


        return expiringAdmins;
    }

    private record ExpiringAdmin(Guid AzureUniqueId, string FullName, DateTimeOffset ValidTo);
}