using Fusion.Testing;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests
{
    internal static class ApiHelpers
    {

        public static async Task DelegateExternalAdminAccessAsync(this HttpClient client, Guid projectId, Guid contractId, Guid peronUniqueId)
        {
            var resp = await client.TestClientPostAsync($"/projects/{projectId}/contracts/{contractId}/delegated-roles", new
            {
                person = new { AzureUniquePersonId = peronUniqueId },
                classification = "External",
                type = "CR"
            });

            resp.Should().BeSuccessfull();
        }
    }
}
