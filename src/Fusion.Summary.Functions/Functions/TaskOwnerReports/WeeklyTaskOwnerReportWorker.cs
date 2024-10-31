using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
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
    private readonly ILogger<WeeklyTaskOwnerReportWorker> logger;

    public WeeklyTaskOwnerReportWorker(ISummaryApiClient summaryApiClient, ILogger<WeeklyTaskOwnerReportWorker> logger)
    {
        this.summaryApiClient = summaryApiClient;
        this.logger = logger;
    }

    private const string FunctionName = "weekly-task-owner-report-worker";

    [FunctionName(FunctionName)]
    public async Task RunAsync(
        [ServiceBusTrigger("%project_summary_weekly_queue%", Connection = "AzureWebJobsServiceBus")]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageReceiver)
    {
        var dto = await JsonSerializer.DeserializeAsync<WeeklyTaskOwnerReportMessage>(message.Body.ToStream());

        logger.LogInformation("{FunctionName} started with message: {MessageBody}", FunctionName, dto.ToJson());
        try
        {
            await CreateAndStoreReportAsync(dto);
            await messageReceiver.CompleteMessageAsync(message);
            logger.LogInformation($"{FunctionName} completed successfully");
        }
        catch (Exception e) // Dead letter message
        {
            logger.LogError(e, $"{FunctionName} completed with error");
            throw;
        }
    }

    private async Task CreateAndStoreReportAsync(WeeklyTaskOwnerReportMessage message)
    {
    }
}