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
    private readonly ILogger<ScheduledReportTimerFunction> _logger;

    private const string QueueName = "queue-name";
    private const string ServiceBusConnectionString = "service-bus-connection-string";

    public ScheduledReportContentBuilderFunction(IPeopleApiClient peopleClient, IResourcesApiClient resourcesClient,
        ILineOrgApiClient lineOrgClient, ILogger<ScheduledReportTimerFunction> logger)
    {
        _resourcesClient = resourcesClient;
        _lineOrgClient = lineOrgClient;
        _peopleClient = peopleClient;
        _logger = logger;
    }
    
        [FunctionName("scheduled-report-content-Builder-function")]
        public async Task RunAsync(
            [ServiceBusTrigger(QueueName, Connection = "AzureWebJobsServiceBus")] ServiceBusReceivedMessage message,
            ILogger log,
            ServiceBusMessageActions messageReceiver,
            [ServiceBus(QueueName, Connection = "AzureWebJobsServiceBus")] IAsyncCollector<ServiceBusMessage> sender)
    {
        log.LogInformation($"function executed at: {DateTime.UtcNow}");
    }
}