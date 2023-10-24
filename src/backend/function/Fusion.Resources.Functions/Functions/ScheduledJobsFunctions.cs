using Fusion.Resources.Functions.ApiClients;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions
{
    public class ScheduledJobsFunctions
    {
        private readonly ILogger<ScheduledJobsFunctions> logger;
        private HttpClient resourcesApiClient;

        public ScheduledJobsFunctions(ILogger<ScheduledJobsFunctions> logger, IHttpClientFactory httpClientFactory)
        {
            resourcesApiClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
            this.logger = logger;
        }

        [Singleton]
        [FunctionName("clear-api-internal-cache")]
        public async Task ReassignResourceAllocationRequestsWithInvalidDepartment([TimerTrigger("0 0 4 * * 0", RunOnStartup = false)] TimerInfo timer)
        {

            var resp = await resourcesApiClient.PostAsync("/admin/cache/reset-internal-cache", null);
            var result = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                logger.LogError($"Error triggering cache reset. Response from service: {result}");
            }

            resp.EnsureSuccessStatusCode();

        }
    }
}
