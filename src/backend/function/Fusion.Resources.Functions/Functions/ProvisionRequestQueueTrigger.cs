using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Integration.Models.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Functions;

public class ProvisionRequestQueueTrigger
{
    private readonly IResourcesApiClient resourcesClient;
    private readonly IConfiguration configuration;
    private readonly INotificationApiClient notificationsClient;


    public ProvisionRequestQueueTrigger(
        IConfiguration configuration,
        IResourcesApiClient resourcesClient,
        INotificationApiClient notificationsClient)
    {
        this.configuration = configuration;
        this.resourcesClient = resourcesClient;
        this.notificationsClient = notificationsClient;
    }

    [FunctionName("provision-position-request")]
    public async Task RunAsync(
        [ServiceBusTrigger("%provision_position_queue%", Connection = "AzureWebJobsServiceBus")]
        ServiceBusReceivedMessage message,
        ILogger log,
        ServiceBusMessageActions messageReceiver,
        [ServiceBus("%provision_position_queue%", Connection = "AzureWebJobsServiceBus")]
        IAsyncCollector<ServiceBusMessage> sender)
    {
        var processor = new QueueMessageProcessor(log, messageReceiver, sender, configuration, resourcesClient,
            notificationsClient);
        await processor.ProcessWithRetriesAsync(message, ProcessMessageAsync);
    }

    private async Task ProcessMessageAsync(string messageBody, ILogger log)
    {
        var payload = JsonSerializer.Deserialize<ProvisionPositionMessageV1>(messageBody);

        var wasProvisioned = payload.Type switch
        {
            ProvisionPositionMessageV1.RequestTypeV1.InternalPersonnel => await ProvisionInternalPersonnel(log,
                payload),
            _ => throw new InvalidOperationException($" {payload.Type} is not a supported provisioning type.")
        };

        log.LogTrace($"Position was provisioned?: {wasProvisioned}");
    }

    private async Task<bool> ProvisionInternalPersonnel(ILogger log, ProvisionPositionMessageV1 payload)
    {
        var provisionResponse =
            await resourcesClient.ProvisionResourceAllocationRequest(payload.RequestId);

        var content = await provisionResponse.Content.ReadAsStringAsync();

        if (!provisionResponse.IsSuccessStatusCode)
        {
            log.LogError($"An error occured when trying to provision the request with id {payload.RequestId}");
            log.LogError(content);

            throw new Exception(
                $"An error occured when trying to provision the request with id {payload.RequestId}");
        }
        else
        {
            log.LogInformation($"Successfully provisioned the position for request {payload.RequestId}");
            log.LogInformation(content);
        }

        return true;
    }
}