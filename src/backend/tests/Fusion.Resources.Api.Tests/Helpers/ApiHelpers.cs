﻿using Bogus;
using Fusion.ApiClients.Org;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Api.Tests.Helpers.Models.Requests;
using Fusion.Resources.Api.Tests.IntegrationTests;
using Fusion.Testing;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests
{
    internal static class ApiHelpers
    {
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
                .WithPosition(position) as ApiCreateInternalRequestModel;

            setup?.Invoke(requestModel);

            var newRequestResponse = await client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{project.Project.ProjectId}/requests", requestModel);
            newRequestResponse.Should().BeSuccessfull();

            return newRequestResponse.Value;
        }

        /// <summary>
        /// Create new request and start it.
        /// </summary>
        /// <returns></returns>
        public static async Task<TestApiInternalRequestModel> CreateAndStartDefaultRequestOnPositionAsync(this HttpClient client, FusionTestProjectBuilder project, ApiPositionV2 position)
        {
            var requestModel = new ApiCreateInternalRequestModel()
                .AsTypeNormal()
                .WithPosition(position);

            var newRequestResponse = await client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{project.Project.ProjectId}/requests", requestModel);
            newRequestResponse.Should().BeSuccessfull();

            var startedRequest = await client.StartProjectRequestAsync(project, newRequestResponse.Value.Id);

            return startedRequest;
        }

        public static async Task<TestApiInternalRequestModel> CreateDefaultResourceOwnerRequestAsync(this HttpClient client, string department, FusionTestProjectBuilder project,
            Action<ApiCreateInternalRequestModel> setup = null, Action<ApiPositionV2> positionSetup = null)
        {
            var position = project.AddPosition()
                .WithInstances(1)
                .WithEnsuredFutureInstances();

            positionSetup?.Invoke(position);

            var requestModel = new ApiCreateInternalRequestModel()
                .AsTypeResourceOwner()
                .WithPosition(position) as ApiCreateInternalRequestModel;

            setup?.Invoke(requestModel);

            var newRequestResponse = await client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{department}/resources/requests", requestModel);
            newRequestResponse.Should().BeSuccessfull();

            return newRequestResponse.Value;
        }

        public static async Task<TestApiInternalRequestModel> AssignDepartmentAsync(this HttpClient client, Guid requestId, string department)
        {
            var newRequestResponse = await client.TestClientPatchAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{requestId}", new
            {
                assignedDepartment = department
            });
            newRequestResponse.Should().BeSuccessfull();

            return newRequestResponse.Value;
        }

        public static async Task<TestApiInternalRequestModel> AssignRandomDepartmentAsync(this HttpClient client, Guid requestId)
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

        public static async Task<TestApiInternalRequestModel> ProposeChangesAsync(this HttpClient client, Guid requestId, object changes)
        {
            var resp = await client.TestClientPatchAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{requestId}", new
            {
                proposedChanges = changes
            });
            resp.Should().BeSuccessfull();

            return resp.Value;
        }

        public static async Task<TestApiInternalRequestModel> SetChangeParamsAsync(this HttpClient client, Guid requestId, DateTime? changeDateFrom, DateTime? changeDateTo = null)
        {
            var resp = await client.TestClientPatchAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{requestId}", new
            {
                proposalParameters = new
                {
                    changeDateFrom = changeDateFrom,
                    changeDateTo = changeDateTo
                }
            });
            resp.Should().BeSuccessfull();

            return resp.Value;
        }

        public static async Task ProvisionRequestAsync(this HttpClient client, Guid requestId)
        {
            var resp = await client.TestClientPostAsync<object>($"/resources/requests/internal/{requestId}/provision", null);
            resp.Should().BeSuccessfull();
        }

        public static async Task<TestApiInternalRequestModel> TaskOwnerApproveAsync(this HttpClient client, FusionTestProjectBuilder project, Guid requestId)
        {
            var resp = await client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{project.Project.ProjectId}/resources/requests/{requestId}/approve", null);
            resp.Should().BeSuccessfull();

            return resp.Value;
        }

        public static async Task<TestApiInternalRequestModel> ResourceOwnerApproveAsync(this HttpClient client, string department, Guid requestId)
        {
            var resp = await client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{department}/resources/requests/{requestId}/approve", null);
            resp.Should().BeSuccessfull();

            return resp.Value;
        }

        public static async Task AddDelegatedDepartmentOwner(this HttpClient client, ApiPersonProfileV3 testUser, string department, DateTime dateFrom, DateTime dateTo)
        {
            var resp = await client.TestClientPostAsync($"/departments/{department}/delegated-resource-owner", new
            {
                responsibleAzureUniqueId = testUser.AzureUniqueId,
                dateFrom,
                dateTo
            });

            resp.Should().BeSuccessfull();
        }

        public static async Task<TestApiRequestAction> AddRequestActionAsync(this HttpClient client, Guid requestId, string responsible = "TaskOwner", Dictionary<string, object> props = null)
        {
            var payload = new
            {
                title = "Test title",
                body = "Test body",
                type = "test",
                subType = "Test Test",
                source = "ResourceOwner",
                responsible = responsible,
                Properties = props
            };

            var result = await client.TestClientPostAsync<TestApiRequestAction>(
                $"/requests/{requestId}/actions", payload
            );

            result.Should().BeSuccessfull();
            return result.Value;
        }

        public static async Task<TestApiRequestAction> AddRequestActionAsync(this HttpClient client, Guid requestId, Action<TestCreateRequestAction> setup, Dictionary<string, object> props = null)
        {
            var payload = new TestCreateRequestAction
            {
                title = "Test title",
                body = "Test body",
                type = "test",
                subType = "Test Test",
                source = "ResourceOwner",
                responsible = "TaskOwner",
                properties = props
            };
            setup(payload);

            var result = await client.TestClientPostAsync<TestApiRequestAction>(
                $"/requests/{requestId}/actions", payload
            );

            result.Should().BeSuccessfull();
            return result.Value;
        }

        public static Task<TestApiRequestMessage> AddRequestMessage(this HttpClient client, Guid requestId, string recipient = "TaskOwner", Dictionary<string, object> props = null)
        {
            return AddRequestMessage(client, requestId,
                new
                {
                    title = "Hello, world!",
                    body = "Goodbye, world!",
                    category = "world",
                    recipient = recipient,
                    properties = props
                }
            );
        }
        public static async Task<TestApiRequestMessage> AddRequestMessage<T>(this HttpClient client, Guid requestId, T payload)
        {
            var result = await client.TestClientPostAsync<TestApiRequestMessage>($"/requests/internal/{requestId}/conversation", payload);
            result.Should().BeSuccessfull();

            return result.Value;
        }


        public static async Task<TestResponsibilitMatrix> AddResponsibilityMatrixAsync(this HttpClient client, FusionTestProjectBuilder testProject, Action<UpdateResponsibilityMatrixRequest> setup = null)
        {
            var request = new UpdateResponsibilityMatrixRequest
            {
                ProjectId = testProject.Project.ProjectId,
                LocationId = Guid.NewGuid(),
                Discipline = "WallaWallaUpdated",
                BasePositionId = testProject.Positions.First().BasePosition.Id,
                Sector = "ABC DEF",
                Unit = "ABC DEF GHI",
            };
            setup?.Invoke(request);

            var response = await client.TestClientPostAsync<TestResponsibilitMatrix>($"/internal-resources/responsibility-matrix", request);
            response.Should().BeSuccessfull();

            return response.Value;
        }

        /// <summary>
        /// Returns a http wrapped response, so consumer can evaluate response
        /// </summary>
        public static async Task<TestClientHttpResponse<TestAbsence>> AddAbsence(this HttpClient client, ApiPersonProfileV3 user, Action<TestAbsence> setup = null)
        {
            var payload = new Faker<TestAbsence>()
                .RuleFor(x => x.AppliesFrom, f => f.Date.Future())
                .RuleFor(x => x.AppliesTo, (f, x) => f.Date.Future(refDate: x.AppliesFrom?.DateTime))
                .RuleFor(x => x.Comment, f => f.Lorem.Sentence())
                .RuleFor(x => x.AbsencePercentage, f => f.Random.Number(0, 100))
                .RuleFor(x => x.Type, f => "OtherTasks")
                .Generate();

            payload.TaskDetails = new Faker<TestTaskDetails>()
                .RuleFor(x => x.TaskName, f => f.Company.CatchPhrase())
                .RuleFor(x => x.RoleName, f => f.Company.CatchPhrase())
                .RuleFor(x => x.Location, f => f.Address.City())
                .Generate();

            setup?.Invoke(payload);
            return await client.TestClientPostAsync<TestAbsence>(
                $"/persons/{user.AzureUniqueId}/absence",
                payload
            );
        }

        /// <summary>
        /// Adds an absence for the user, defaults to 'OtherTask'. Ensures successfull response and returns created absence.
        /// </summary>
        public static async Task<TestAbsence> AddUserOtherTask(this HttpClient client, ApiPersonProfileV3 user, Action<TestAbsence> setup = null)
        {
            var payload = new Faker<TestAbsence>()
                .RuleFor(x => x.AppliesFrom, f => f.Date.Future())
                .RuleFor(x => x.AppliesTo, (f, x) => f.Date.Future(refDate: x.AppliesFrom?.DateTime))
                .RuleFor(x => x.Comment, f => f.Lorem.Sentence())
                .RuleFor(x => x.AbsencePercentage, f => f.Random.Number(0, 100))
                .RuleFor(x => x.Type, f => "OtherTasks")
                .Generate();

            payload.TaskDetails = new Faker<TestTaskDetails>()
                .RuleFor(x => x.TaskName, f => f.Company.CatchPhrase())
                .RuleFor(x => x.RoleName, f => f.Company.CatchPhrase())
                .Generate();

            setup?.Invoke(payload);

            var resp = await client.TestClientPostAsync<TestAbsence>($"/persons/{user.AzureUniqueId}/absence", payload);
            resp.Should().BeSuccessfull();
            return resp.Value;
        }

        /// <summary>
        /// Adds an absence for the user, of type 'Absence'. Ensures successfull response and returns created absence.
        /// </summary>
        public static async Task<TestAbsence> AddUserAbsence(this HttpClient client, ApiPersonProfileV3 user, Action<TestAbsence> setup = null)
        {
            var payload = new Faker<TestAbsence>()
                .RuleFor(x => x.AppliesFrom, f => f.Date.Future())
                .RuleFor(x => x.AppliesTo, (f, x) => f.Date.Future(refDate: x.AppliesFrom?.DateTime))
                .RuleFor(x => x.Comment, f => f.Lorem.Sentence())
                .RuleFor(x => x.AbsencePercentage, f => f.Random.Number(0, 100))
                .RuleFor(x => x.Type, f => "Absence")
                .Generate();

            setup?.Invoke(payload);

            var resp = await client.TestClientPostAsync<TestAbsence>($"/persons/{user.AzureUniqueId}/absence", payload);
            resp.Should().BeSuccessfull();
            return resp.Value;
        }

        public static async Task<TestClientHttpResponse<dynamic>> ShareRequest(this HttpClient client, Guid requestId, ApiPersonProfileV3 user)
        {
            var endpoint = $"/resources/requests/internal/{requestId}/share";

            var share = new
            {
                scope = "Basic.Read",
                reason = "Test request sharing",
                sharedWith = new[]
                {
                    new { mail = user.Mail }
                }
            };
            return await client.TestClientPostAsync(endpoint, share);
        }
    }
}
