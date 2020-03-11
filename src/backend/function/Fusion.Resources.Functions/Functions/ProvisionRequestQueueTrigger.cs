using System;
using System.Net.Http;
using System.Text.Json;
using Fusion.Resources.Integration.Models.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
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
        public async System.Threading.Tasks.Task RunAsync([ServiceBusTrigger("%provision_position_queue%", Connection = "AzureWebJobsServiceBus")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            var payload = JsonSerializer.Deserialize<ProvisionPositionMessageV1>(myQueueItem);

            if (payload.Type == ProvisionPositionMessageV1.RequestTypeV1.ContractorPersonnel)
            {
                var provisionResponse = await resourcesClient.PostAsync($"/projects/{payload.ProjectOrgId}/contracts/{payload.ContractOrgId}/resources/requests/{payload.RequestId}", null);

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
            }

        }
    }

    
    
}
