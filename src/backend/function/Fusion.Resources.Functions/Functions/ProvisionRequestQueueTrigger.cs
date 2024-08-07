using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.Common.Integration.Http;
using Fusion.Resources.Integration.Models.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Functions
{
    public class ProvisionRequestQueueTrigger
    {
        private readonly HttpClient resourcesClient;


        public ProvisionRequestQueueTrigger(IHttpClientFactory httpClientFactory)
        {
            this.resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
        }

        [FunctionName("provision-position-request")]
        public async Task RunAsync(
            [ServiceBusTrigger("%provision_position_queue%", Connection = "AzureWebJobsServiceBus")] ServiceBusReceivedMessage message,
            ILogger log,
            ServiceBusMessageActions messageReceiver,
            [ServiceBus("%provision_position_queue%", Connection = "AzureWebJobsServiceBus")] IAsyncCollector<ServiceBusMessage> sender)
        {
            var processor = new QueueMessageProcessor(log, messageReceiver, sender);
            await processor.ProcessWithRetriesAsync(message, ProcessMessageAsync);
        }

        private async Task ProcessMessageAsync(string messageBody, ILogger log)
        {
            var payload = JsonSerializer.Deserialize<ProvisionPositionMessageV1>(messageBody);

            var wasProvisioned = payload.Type switch
            {
                ProvisionPositionMessageV1.RequestTypeV1.InternalPersonnel => await ProvisionInternalPersonnel(log, payload),
                _ => throw new InvalidOperationException($" {payload.Type} is not a supported provisioning type.")
            };

            log.LogTrace($"Position was provisioned?: {wasProvisioned}");
        }
        
        private async Task<bool> ProvisionInternalPersonnel(ILogger log, ProvisionPositionMessageV1 payload)
        {
            var provisionResponse =
                await resourcesClient.PostAsync($"/resources/requests/internal/{payload.RequestId}/provision", null);

            var content = await provisionResponse.Content.ReadAsStringAsync();

            if (!provisionResponse.IsSuccessStatusCode)
            {
                log.LogError($"An error occured when trying to provision the request with id {payload.RequestId}");
                log.LogError(content);

                throw new Exception($"An error occured when trying to provision the request with id {payload.RequestId}");
            }
            else
            {
                log.LogInformation($"Successfully provisioned the position for request {payload.RequestId}");
                log.LogInformation(content);
            }

            return true;
        }

    }

}
