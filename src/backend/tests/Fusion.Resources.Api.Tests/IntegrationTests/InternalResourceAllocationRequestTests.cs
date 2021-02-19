using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class InternalResourceAllocationRequestTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;

        /// <summary>
        ///     Will be generated new for each test
        /// </summary>
        private readonly ApiPersonProfileV3 testUser;


        // Created by the async lifetime
        private FusionTestResourceAllocationBuilder testRequest;

        public InternalResourceAllocationRequestTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.External);
        }

        private HttpClient Client => fixture.ApiFactory.CreateClient();

        public async Task InitializeAsync()
        {
            // Mock profile
            var testProfile = PeopleServiceMock.AddTestProfile()
                .SaveProfile();

            // Mock project
            var testProject = new FusionTestProjectBuilder()
                .WithPositions(1)
                .AddToMockService();

            // Prepare project with mocks
            testRequest = new FusionTestResourceAllocationBuilder()
                    .WithRequestType(ApiAllocationRequestType.Direct)
                    .WithOrgPositionId(testProject.Positions.First())
                    .WithProposedPerson(testProfile)
                    .WithIsDraft(true)
                    .WithProposedChanges(new ApiPropertiesCollection { { "PROPA", "CHANGEA" }, { "PROPB", "CHANGEB" } })
                    .WithProject(testProject.Project)
                ;

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(testRequest.Project);

            // Prepare admin client
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            // Make sure we are able to create a request
            var response = await adminClient.TestClientPostAsync($"/projects/{testRequest.Project.ProjectId}/requests", testRequest.Request, new { Id = Guid.Empty });
            response.Should().BeSuccessfull();
            testRequest.Request.Id = response.Value.Id;
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }

        [Fact]
        public async Task CreateRequest_Invalid_Request_InvalidArguments_ShouldBe_BadRequest()
        {
            testRequest.Request.OrgPositionId = Guid.Empty;

            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientPostAsync($"/projects/{testRequest.Project.ProjectId}/requests", testRequest.Request, new { Id = Guid.Empty });

            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task DeleteRequest_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientDeleteAsync($"/projects/{testRequest.Project.ProjectId}/requests/{testRequest.Request.Id}");
            response.Should().BeSuccessfull();
        }
        [Fact]
        public async Task Delete_NonExisting_Request_Using_AdminRole_ShouldBe_NotFound()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientDeleteAsync($"/projects/{testRequest.Project.ProjectId}/requests/{Guid.NewGuid()}");
            response.Should().BeNotFound();
        }

        [Fact]
        public async Task GetProjectRequest_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<ResourceAllocationRequestTestModel>($"/projects/{testRequest.Project.ProjectId}/requests/{testRequest.Request.Id}");
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, testRequest.Request, adminScope);
        }

        [Fact]
        public async Task GetProjectRequests_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();

            for (int i = 0; i < 150; i++)
            {
                var r = await Client.TestClientPostAsync($"/projects/{testRequest.Project.ProjectId}/requests", testRequest.Request, new { Id = Guid.Empty });
                r.Should().BeSuccessfull();
            }

            for (int j = 5; j < 20; j++)
            {
                var topResponseTest = await Client.TestClientGetAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/projects/{testRequest.Project.ProjectId}/requests?$search={testRequest.Request.AssignedDepartment}&$filter=assignedDepartment eq '{testRequest.Request.AssignedDepartment}'&$skip=2&$top={j}");
                topResponseTest.Should().BeSuccessfull();
                topResponseTest.Value.Value.Count().Should().Be(j);
            }

            var response = await Client.TestClientGetAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/projects/{testRequest.Project.ProjectId}/requests");
            response.Should().BeSuccessfull();

            response.Value.Value.Count().Should().Be(100); // Default page size is 100

        }

        [Fact]
        public async Task GetRequest_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<ResourceAllocationRequestTestModel>($"/resources/internal-requests/requests/{testRequest.Request.Id}");
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, testRequest.Request, adminScope);
        }

        [Fact]
        public async Task GetRequests_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/resources/internal-requests/requests");
            response.Should().BeSuccessfull();

            response.Value.Value.Count().Should().BeGreaterThan(0);

        }
        [Fact]
        public async Task PutProjectRequest_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var beforeUpdate = DateTimeOffset.UtcNow;
            var updateRequest = new UpdateResourceAllocationRequest
            {
                ProjectId = testRequest.Project.ProjectId,
                OrgPositionId = testRequest.Request.OrgPositionId,
                OrgPositionInstance = testRequest.Request.OrgPositionInstance,
                Type = ApiAllocationRequestType.JointVenture,
                Discipline = "upd",
                IsDraft = false,
                AdditionalNote = "upd",
                ProposedPersonAzureUniqueId = testRequest.Request.ProposedPersonAzureUniqueId
            };

            var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/projects/{testRequest.Project.ProjectId}/requests/{testRequest.Request.Id}", updateRequest);
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, updateRequest, adminScope);
            response.Value.Updated?.Should().BeAfter(beforeUpdate);
        }
        [Fact]
        public async Task PutRequest_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var beforeUpdate = DateTimeOffset.UtcNow;

            var updateRequest = new UpdateResourceAllocationRequest
            {
                ProjectId = testRequest.Project.ProjectId,
                OrgPositionId = testRequest.Request.OrgPositionId,
                OrgPositionInstance = testRequest.Request.OrgPositionInstance,
                Type = ApiAllocationRequestType.Normal,
                Discipline = "upd",
                IsDraft = false,
                AdditionalNote = "upd",
                ProposedPersonAzureUniqueId = testRequest.Request.ProposedPersonAzureUniqueId
            };

            var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/resources/internal-requests/requests/{testRequest.Request.Id}", updateRequest);
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, updateRequest, adminScope);
            response.Value.Updated?.Should().BeAfter(beforeUpdate);
        }

        [Fact]
        public async Task PutRequest_InvalidRequest_ShouldBe_Unsuccessful()
        {
            using var adminScope = fixture.AdminScope();

            var updateRequest = new UpdateResourceAllocationRequest { ProposedPersonAzureUniqueId = Guid.Empty };
            var response = await Client.TestClientPutAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/projects/{testRequest.Project.ProjectId}/requests/{testRequest.Request.Id}", updateRequest);
            response.Should().BeBadRequest("Invalid arguments passed");

        }
        [Fact]
        public async Task PutAdminRequest_RequestMissingProjectId_ShouldBe_Unsuccessful()
        {
            using var adminScope = fixture.AdminScope();
            var updateRequest = new UpdateResourceAllocationRequest { ProposedPersonAzureUniqueId = Guid.Empty };
            var response = await Client.TestClientPutAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/resources/internal-requests/requests/{testRequest.Request.Id}", updateRequest);
            response.Should().BeBadRequest("ProjectId argument missing");

        }

        [Fact]
        public async Task PostRequest_InvalidRequest_ShouldBe_Unsuccessful()
        {
            using var adminScope = fixture.AdminScope();
            var updateRequest = new CreateResourceAllocationRequest();
            var response = await Client.TestClientPostAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/projects/{testRequest.Project.ProjectId}/requests", updateRequest);
            response.Should().BeBadRequest("Invalid arguments passed");

        }


        public class PagedCollection<T>
        {
            public PagedCollection(IEnumerable<T> items)
            {
                Value = items;
            }

            public IEnumerable<T> Value { get; set; }
        }

        public class ResourceAllocationRequestTestModel
        {
            public string AssignedDepartment { get; set; }
            public string Discipline { get; set; }
            public ObjectWithId Project { get; set; }
            public string Type { get; set; }
            public ObjectWithId OrgPosition { get; set; }
            public ObjectWithId OrgPositionInstance { get; set; }
            public string AdditionalNote { get; set; }
            public bool? IsDraft { get; set; }
            public Dictionary<string, object> ProposedChanges { get; set; }
            public ObjectWithAzureUniquePerson ProposedPerson { get; set; }

            public ObjectWithAzureUniquePerson CreatedBy { get; set; }
            public ObjectWithAzureUniquePerson UpdatedBy { get; set; }

            public DateTimeOffset? Created { get; set; }
            public DateTimeOffset? Updated { get; set; }
            public DateTimeOffset? LastActivity { get; set; }

            public class ObjectWithAzureUniquePerson
            {
                public Guid? AzureUniquePersonId { get; set; }
            }

            public class ObjectWithId
            {
                public Guid? Id { get; set; }
            }
        }

        private static void AssertPropsAreEqual(ResourceAllocationRequestTestModel response,
            CreateResourceAllocationRequest request, TestClientScope scope)
        {

            response.Type.Should().Be(request.Type.ToString());
            response.AssignedDepartment.Should().Be(request.AssignedDepartment);
            response.Discipline.Should().Be(request.Discipline);

            response.Project.Id.Should().Be(request.ProjectId);
            response.OrgPosition?.Id.Should().Be(request.OrgPositionId);
            response.OrgPositionInstance?.Id.Should().Be(request.OrgPositionInstance?.Id);
            response.ProposedPerson.AzureUniquePersonId.Should().Be(request.ProposedPersonAzureUniqueId);
            response.AdditionalNote.Should().Be(request.AdditionalNote);
            response.IsDraft.Should().Be(request.IsDraft);
            if (request.ProposedChanges != null)
                foreach (var (key, value) in request.ProposedChanges)
                {
                    response.ProposedChanges.Should().ContainValue(value);
                }

            response.CreatedBy.AzureUniquePersonId.Should().Be(scope.Profile.AzureUniqueId);
            response.Created.Should().NotBeNull();
            response.LastActivity.Should().NotBeNull();

            //Workflow/state & provisioning status to be added.
        }
        private static void AssertPropsAreEqual(ResourceAllocationRequestTestModel response,
            UpdateResourceAllocationRequest request, TestClientScope scope)
        {

            response.Type.Should().Be(request.Type.ToString());
            response.AssignedDepartment.Should().Be(request.AssignedDepartment);
            response.Discipline.Should().Be(request.Discipline);

            response.Project.Id.Should().Be(request.ProjectId);
            response.OrgPosition?.Id.Should().Be(request.OrgPositionId);
            response.OrgPositionInstance?.Id.Should().Be(request.OrgPositionInstance?.Id);
            response.ProposedPerson.AzureUniquePersonId.Should().Be(request.ProposedPersonAzureUniqueId);
            response.AdditionalNote.Should().Be(request.AdditionalNote);
            response.IsDraft.Should().Be(request.IsDraft);
            if (request.ProposedChanges != null)
                foreach (var (key, value) in request.ProposedChanges)
                {
                    response.ProposedChanges.Should().ContainValue(value);
                }

            response.CreatedBy.AzureUniquePersonId.Should().Be(scope.Profile.AzureUniqueId);
            response.Created.Should().NotBeNull();
            response.LastActivity.Should().NotBeNull();

            //Workflow/state & provisioning status to be added.
        }
    }
}