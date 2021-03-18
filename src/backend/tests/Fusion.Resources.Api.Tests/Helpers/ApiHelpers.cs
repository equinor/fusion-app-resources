using Fusion.ApiClients.Org;
using Fusion.Integration.Profile.ApiClient;
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


        public static async Task<TestApiInternalRequestModel> CreateRequestAsync(this HttpClient client, Guid projectId, Action<ApiCreateInternalRequestModel> setup)
        {
            var model = new ApiCreateInternalRequestModel();

            setup(model);

            var newRequestResponse = await client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests", model);
            newRequestResponse.Should().BeSuccessfull();

            return newRequestResponse.Value;
        }


        public static async Task<TestApiInternalRequestModel> CreateDefaultRequestAsync(this HttpClient client, FusionTestProjectBuilder project, 
            Action<ApiCreateInternalRequestModel> setup = null, Action<ApiPositionV2> positionSetup = null)
        {
            var position = project.AddPosition();

            positionSetup?.Invoke(position);

            var requestModel = new ApiCreateInternalRequestModel()
                .AsTypeNormal()
                .WithPosition(position);

            setup?.Invoke(requestModel);

            var newRequestResponse = await client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{project.Project.ProjectId}/requests", requestModel);
            newRequestResponse.Should().BeSuccessfull();

            return newRequestResponse.Value;
        }

        public static async Task<TestApiInternalRequestModel> CreateDefaultResourceOwnerRequestAsync(this HttpClient client, string department, FusionTestProjectBuilder project,
            Action<ApiCreateInternalRequestModel> setup = null, Action<ApiPositionV2> positionSetup = null)
        {
            var position = project.AddPosition();

            positionSetup?.Invoke(position);

            var requestModel = new ApiCreateInternalRequestModel()
                .AsTypeResourceOwner()
                .WithPosition(position);

            setup?.Invoke(requestModel);

            var newRequestResponse = await client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{department}/resources/requests", requestModel);
            newRequestResponse.Should().BeSuccessfull();

            return newRequestResponse.Value;
        }

        public static async Task<TestApiInternalRequestModel> AssignDepartmentAsync(this HttpClient client, Guid requestId, string? department)
        {
            var newRequestResponse = await client.TestClientPatchAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{requestId}", new
            {
                assignedDepartment = department
            });
            newRequestResponse.Should().BeSuccessfull();

            return newRequestResponse.Value;
        }

        public static async Task<TestApiInternalRequestModel> AssignAnDepartmentAsync(this HttpClient client, Guid requestId)
        {
            var newRequestResponse = await client.TestClientPatchAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{requestId}", new
            {
                assignedDepartment = InternalRequestData.RandomDepartment
            });
            newRequestResponse.Should().BeSuccessfull();

            return newRequestResponse.Value;
        }

        public static async Task<TestApiInternalRequestModel> StartProjectRequestAsync(this HttpClient client, FusionTestProjectBuilder project, Guid requestId)
        {
            var newRequestResponse = await client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{project.Project.ProjectId}/requests/{requestId}/start", null);
            newRequestResponse.Should().BeSuccessfull();

            return newRequestResponse.Value;
        }

        public static async Task<TestApiInternalRequestModel> ProposePersonAsync(this HttpClient client, Guid requestId, ApiPersonProfileV3 profile)
        {
            var resp = await client.TestClientPatchAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{requestId}", new
            {
                proposedPersonAzureUniqueId = profile.AzureUniqueId
            });
            resp.Should().BeSuccessfull();

            return resp.Value;
        }
    }
}
