using Fusion.Testing;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
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


        public static async Task<Guid> CreateRequestAsync(this HttpClient client, Guid projectId, Action<FusionTestResourceAllocationBuilder> setup)
        {
            var builder = new FusionTestResourceAllocationBuilder();

            setup(builder);

            var newRequestResponse = await client.TestClientPostAsync($"/projects/{projectId}/requests", builder.Request, new { Id = Guid.Empty });
            newRequestResponse.Should().BeSuccessfull();

            return newRequestResponse.Value.Id;
        }

        public static async Task<Guid> CreateRequestAsync(this HttpClient client, FusionTestProjectBuilder project, Action<FusionTestResourceAllocationBuilder> setup)
        {
            var position = project.AddPosition();

            var builder = new FusionTestResourceAllocationBuilder()
                .WithOrgPositionId(position)
                .WithProject(project.Project);

            setup(builder);

            var newRequestResponse = await client.TestClientPostAsync($"/projects/{project.Project.ProjectId}/requests", builder.Request, new { Id = Guid.Empty });
            newRequestResponse.Should().BeSuccessfull();

            return newRequestResponse.Value.Id;
        }
    }
}
