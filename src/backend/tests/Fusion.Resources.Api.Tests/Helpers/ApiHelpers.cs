using Fusion.Testing;
using System;
using System.Net.Http;
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

        public static async Task DelegateInternalAdminAccessAsync(this HttpClient client, Guid projectId, Guid contractId, Guid peronUniqueId)
        {
            var resp = await client.TestClientPostAsync($"/projects/{projectId}/contracts/{contractId}/delegated-roles", new
            {
                person = new { AzureUniquePersonId = peronUniqueId },
                classification = "Internal",
                type = "CR"
            });

            resp.Should().BeSuccessfull();
        }
    }
}
