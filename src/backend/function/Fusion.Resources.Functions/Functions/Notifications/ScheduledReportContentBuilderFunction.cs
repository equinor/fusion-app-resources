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
    private readonly IResourcesApiClient _resourcesClient;
    private readonly IPeopleApiClient _peopleClient;
    private readonly ILineOrgApiClient _lineOrgClient;
    private readonly ILogger<ScheduledReportContentBuilderFunction> _logger;

    public ScheduledReportContentBuilderFunction(IPeopleApiClient peopleClient, IResourcesApiClient resourcesClient,
        ILineOrgApiClient lineOrgClient, ILogger<ScheduledReportContentBuilderFunction> logger)
    {
        _resourcesClient = resourcesClient;
        _lineOrgClient = lineOrgClient;
        _peopleClient = peopleClient;
        _logger = logger;
    }

    [FunctionName(ScheduledReportFunctionSettings.ContentBuilderFunctionName)]
    public async Task RunAsync(
        [ServiceBusTrigger(ScheduledReportServiceBusSettings.QueueName, Connection = "AzureWebJobsServiceBus")]
        ServiceBusReceivedMessage message)
    {
        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' started with message: {message.Body}");
        if (!Guid.TryParse(message.Body.ToString(), out var positionId))
        {
            _logger.LogError(
                $"ServiceBus queue '{ScheduledReportServiceBusSettings.QueueName}', error receiving message: positionId is empty");
            return;
        }

        await BuildContent(positionId);

        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' finished with message: {message.Body}");
    }

    private async Task BuildContent(Guid positionId)
    {
        throw new NotImplementedException();
    }
}