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
    public class ResourceAllocationTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;

        /// <summary>
        ///     Will be generated new for each test
        /// </summary>
        private readonly ApiPersonProfileV3 testUser;


        // Created by the async lifetime
        private FusionTestResourceAllocationBuilder testRequest;

        public ResourceAllocationTests(ResourceApiFixture fixture, ITestOutputHelper output)
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
        public async Task DeleteRequest_RandomRole_ShouldBe_Unauthorized()
        {
            using var userScope = fixture.UserScope(testUser);
            var response = await Client.TestClientDeleteAsync($"/projects/{testRequest.Project.ProjectId}/requests/{testRequest.Request.Id}");
            response.Should().BeUnauthorized();
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

            AssertPropsAreEqual(response.Value, testRequest, adminScope);
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
                var topResponseTest = await Client.TestClientGetAsync<IEnumerable<ResourceAllocationRequestTestModel>>($"/projects/{testRequest.Project.ProjectId}/requests?$search={testRequest.Request.Discipline}&$filter=discipline eq '{testRequest.Request.Discipline}'&$skip=2&$top={j}");
                topResponseTest.Should().BeSuccessfull();
                topResponseTest.Value.Count().Should().Be(j);
            }

            var response = await Client.TestClientGetAsync<IEnumerable<ResourceAllocationRequestTestModel>>($"/projects/{testRequest.Project.ProjectId}/requests");
            response.Should().BeSuccessfull();

            response.Value.Count().Should().Be(100); // Default page size is 100

        }

        [Fact]
        public async Task GetRequest_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<ResourceAllocationRequestTestModel>($"/resources/internal-requests/requests/{testRequest.Request.Id}");
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, testRequest, adminScope);
        }

        [Fact]
        public async Task GetRequests_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<IEnumerable<ResourceAllocationRequestTestModel>>($"/resources/internal-requests/requests");
            response.Should().BeSuccessfull();

            response.Value.Count().Should().BeGreaterThan(0);

        }
        [Fact]
        public async Task PutRequest_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var beforeUpdate = DateTimeOffset.UtcNow;

            testRequest.Request.Discipline += "upd";
            testRequest.Request.IsDraft = false;
            testRequest.Request.AdditionalNote += "upd";
            testRequest.Request.ProposedChanges?.Add("propUpd", "Updated");
            var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/projects/{testRequest.Project.ProjectId}/requests/{testRequest.Request.Id}", testRequest.Request);
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, testRequest, adminScope);
            response.Value.Updated?.Should().BeAfter(beforeUpdate);
        }

        [Fact]
        public async Task PutRequest_InvalidRequest_ShouldBe_Unsuccessful()
        {
            using var adminScope = fixture.AdminScope();
            var beforeUpdate = DateTimeOffset.UtcNow;

            testRequest.Request.OrgPositionInstance!.AppliesFrom = testRequest.Request.OrgPositionInstance.AppliesTo.AddDays(1);
            testRequest.Request.ProposedPersonAzureUniqueId = Guid.Empty;
            var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/projects/{testRequest.Project.ProjectId}/requests/{testRequest.Request.Id}", testRequest.Request);
            response.Should().BeBadRequest("Invalid arguments passed");

        }

        [Fact]
        public async Task PostRequest_InvalidRequest_ShouldBe_Unsuccessful()
        {
            using var adminScope = fixture.AdminScope();
            testRequest.Request.OrgPositionInstance!.AppliesFrom = testRequest.Request.OrgPositionInstance.AppliesTo.AddDays(1);
            testRequest.Request.ProposedPersonAzureUniqueId = Guid.Empty;
            var response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{testRequest.Project.ProjectId}/requests", testRequest.Request);
            response.Should().BeBadRequest("Invalid arguments passed");

        }

        [Fact]
        public async Task PostRequest_Minimal_Request_ShouldBe_Successful()
        {
            using var adminScope = fixture.AdminScope();
            var minimalRequest = new CreateProjectAllocationRequest();
            var response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{testRequest.Project.ProjectId}/requests", minimalRequest);
            response.Should().BeSuccessfull();

        }

        public class ResourceAllocationRequestTestModel
        {
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
            FusionTestResourceAllocationBuilder request, TestClientScope scope)
        {

            response.Type.Should().Be(request.Request.Type.ToString());
            response.Discipline.Should().Be(request.Request.Discipline);

            response.Project.Id.Should().Be(request.Project.ProjectId);
            response.OrgPosition?.Id.Should().Be(request.Request.OrgPositionId);
            response.OrgPositionInstance?.Id.Should().Be(request.Request.OrgPositionInstance?.Id);
            response.ProposedPerson.AzureUniquePersonId.Should().Be(request.Request.ProposedPersonAzureUniqueId);
            response.AdditionalNote.Should().Be(request.Request.AdditionalNote);
            response.IsDraft.Should().Be(request.Request.IsDraft);
            if (request.Request.ProposedChanges != null)
                foreach (var (key, value) in request.Request.ProposedChanges)
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