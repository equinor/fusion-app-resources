using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportContentBuilderFunction
{
    private readonly ILogger<ScheduledReportContentBuilderFunction> _logger;

    public ScheduledReportContentBuilderFunction(ILogger<ScheduledReportContentBuilderFunction> logger)
    {
        _logger = logger;
    }

    [FunctionName(ScheduledReportFunctionSettings.ContentBuilderFunctionName)]
    public async Task RunAsync(
        [ServiceBusTrigger(ScheduledReportServiceBusSettings.QueueName, Connection = "AzureWebJobsServiceBus")]
        ServiceBusReceivedMessage message)
    {
        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' started with message: {message.Body}");
        if (!Guid.TryParse(message.Body.ToString(), out var azureId))
        {
            _logger.LogError(
                $"ServiceBus queue '{ScheduledReportServiceBusSettings.QueueName}', error receiving message: azureId not valid");
            return;
        }

        await BuildContent(azureId);

        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' finished with message: {message.Body}");
    }

    private async Task BuildContent(Guid azureId)
    {
        throw new NotImplementedException();
    }
}