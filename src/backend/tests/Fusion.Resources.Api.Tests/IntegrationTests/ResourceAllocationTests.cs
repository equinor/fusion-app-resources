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
        private FusionTestResourceAllocationBuilder directTestRequest;
        private FusionTestResourceAllocationBuilder jointVentureTestRequest;
        private FusionTestResourceAllocationBuilder normalTestRequest;

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
            directTestRequest = new FusionTestResourceAllocationBuilder()
                    .WithRequestType(ApiAllocationRequestType.Direct)
                    .WithOrgPositionId(testProject.Positions.First())
                    .WithProposedPerson(testProfile)
                    .WithIsDraft(true)
                    .WithProposedChanges(new ApiPropertiesCollection { { "PROPA", "CHANGEA" }, { "PROPB", "CHANGEB" } })
                    .WithProject(testProject.Project)
                ;
            jointVentureTestRequest = new FusionTestResourceAllocationBuilder()
                    .WithRequestType(ApiAllocationRequestType.JointVenture)
                    .WithOrgPositionId(testProject.Positions.First())
                    .WithProposedPerson(testProfile)
                    .WithIsDraft(true)
                    .WithProposedChanges(new ApiPropertiesCollection { { "PROPA", "CHANGEA" }, { "PROPB", "CHANGEB" } })
                    .WithProject(testProject.Project)
                ;
            normalTestRequest = new FusionTestResourceAllocationBuilder()
                    .WithRequestType(ApiAllocationRequestType.Normal)
                    .WithOrgPositionId(testProject.Positions.First())
                    .WithProposedPerson(testProfile)
                    .WithIsDraft(true)
                    .WithProposedChanges(new ApiPropertiesCollection { { "PROPA", "CHANGEA" }, { "PROPB", "CHANGEB" } })
                    .WithProject(testProject.Project)
                ;

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(directTestRequest.Project);
            fixture.ContextResolver
                .AddContext(jointVentureTestRequest.Project);
            fixture.ContextResolver
                .AddContext(normalTestRequest.Project);

            // Prepare admin client
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            // Make sure we are able to create a request
            var response = await adminClient.TestClientPostAsync($"/projects/{directTestRequest.Project.ProjectId}/requests", directTestRequest.Request, new { Id = Guid.Empty });
            response.Should().BeSuccessfull();
            directTestRequest.Request.Id = response.Value.Id;
            response = await adminClient.TestClientPostAsync($"/projects/{jointVentureTestRequest.Project.ProjectId}/requests", jointVentureTestRequest.Request, new { Id = Guid.Empty });
            response.Should().BeSuccessfull();
            jointVentureTestRequest.Request.Id = response.Value.Id;
            response = await adminClient.TestClientPostAsync($"/projects/{normalTestRequest.Project.ProjectId}/requests", normalTestRequest.Request, new { Id = Guid.Empty });
            response.Should().BeSuccessfull();
            normalTestRequest.Request.Id = response.Value.Id;
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }

        [Fact]
        public async Task CreateRequest_Invalid_Request_InvalidArguments_ShouldBe_BadRequest()
        {
            directTestRequest.Request.OrgPositionId = Guid.Empty;

            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientPostAsync($"/projects/{directTestRequest.Project.ProjectId}/requests", directTestRequest.Request, new { Id = Guid.Empty });

            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task DeleteRequest_RandomRole_ShouldBe_Unauthorized()
        {
            using var userScope = fixture.UserScope(testUser);
            var response = await Client.TestClientDeleteAsync($"/projects/{directTestRequest.Project.ProjectId}/requests/{directTestRequest.Request.Id}");
            response.Should().BeUnauthorized();
        }

        [Fact]
        public async Task DeleteRequest_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientDeleteAsync($"/projects/{directTestRequest.Project.ProjectId}/requests/{directTestRequest.Request.Id}");
            response.Should().BeSuccessfull();
        }
        [Fact]
        public async Task Delete_NonExisting_Request_Using_AdminRole_ShouldBe_NotFound()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientDeleteAsync($"/projects/{directTestRequest.Project.ProjectId}/requests/{Guid.NewGuid()}");
            response.Should().BeNotFound();
        }

        [Fact]
        public async Task GetRequest_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<ResourceAllocationRequestTestModel>($"/projects/{directTestRequest.Project.ProjectId}/requests/{directTestRequest.Request.Id}");
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, directTestRequest, adminScope);
        }

        [Fact]
        public async Task GetRequests_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<IEnumerable<ResourceAllocationRequestTestModel>>($"/projects/{directTestRequest.Project.ProjectId}/requests");
            response.Should().BeSuccessfull();

            response.Value.Count().Should().BeGreaterThan(0);

        }
        [Fact]
        public async Task PutRequest_AdminRole_ShouldBe_Authorized()
        {
            using var adminScope = fixture.AdminScope();
            var beforeUpdate = DateTimeOffset.UtcNow;

            directTestRequest.Request.Discipline += "upd";
            directTestRequest.Request.IsDraft = false;
            directTestRequest.Request.AdditionalNote += "upd";
            directTestRequest.Request.ProposedChanges?.Add("propUpd", "Updated");
            var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/projects/{directTestRequest.Project.ProjectId}/requests/{directTestRequest.Request.Id}", directTestRequest.Request);
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, directTestRequest, adminScope);
            response.Value.Updated?.Should().BeAfter(beforeUpdate);
        }

        [Fact]
        public async Task PutRequest_InvalidRequest_ShouldBe_Unsuccessful()
        {
            using var adminScope = fixture.AdminScope();
            var beforeUpdate = DateTimeOffset.UtcNow;

            directTestRequest.Request.OrgPositionInstance!.AppliesFrom = directTestRequest.Request.OrgPositionInstance.AppliesTo.AddDays(1);
            directTestRequest.Request.ProposedPersonAzureUniqueId = Guid.Empty;
            var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/projects/{directTestRequest.Project.ProjectId}/requests/{directTestRequest.Request.Id}", directTestRequest.Request);
            response.Should().BeBadRequest("Invalid arguments passed");

        }

        [Fact]
        public async Task PostRequest_InvalidRequest_ShouldBe_Unsuccessful()
        {
            using var adminScope = fixture.AdminScope();
            directTestRequest.Request.OrgPositionInstance!.AppliesFrom = directTestRequest.Request.OrgPositionInstance.AppliesTo.AddDays(1);
            directTestRequest.Request.ProposedPersonAzureUniqueId = Guid.Empty;
            var response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{directTestRequest.Project.ProjectId}/requests", directTestRequest.Request);
            response.Should().BeBadRequest("Invalid arguments passed");

        }

        [Fact]
        public async Task PostRequest_Minimal_Request_ShouldBe_Successful()
        {
            using var adminScope = fixture.AdminScope();
            var minimalRequest = new CreateProjectAllocationRequest();
            var response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{directTestRequest.Project.ProjectId}/requests", minimalRequest);
            response.Should().BeSuccessfull();

        }

        [Fact]
        public async Task Post_Approve_Direct_Request_ShouldBe_Successful()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{directTestRequest.Project.ProjectId}/requests/{directTestRequest.Request.Id}/approve", null);
            response.Should().BeSuccessfull();

        }   
        [Fact]
        public async Task Post_Approve_JointVenture_Request_ShouldBe_Successful()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{jointVentureTestRequest.Project.ProjectId}/requests/{jointVentureTestRequest.Request.Id}/approve", null);
            response.Should().BeSuccessfull();

        }   
        [Fact]
        public async Task Post_Approve_Normal_Request_ShouldBe_Successful()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{normalTestRequest.Project.ProjectId}/requests/{normalTestRequest.Request.Id}/approve", null);
            response.Should().BeSuccessfull();

            response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{normalTestRequest.Project.ProjectId}/requests/{normalTestRequest.Request.Id}/approve", null);
            response.Should().BeSuccessfull();


        }   
        [Fact]
        public async Task Post_Terminate_Direct_Request_ShouldBe_Successful()
        {
            using var adminScope = fixture.AdminScope();
            var reason = new RejectRequestRequest() { Reason = "Testing termination" };
            var response2 = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{directTestRequest.Project.ProjectId}/requests/{directTestRequest.Request.Id}/terminate", reason);
            response2.Should().BeSuccessfull();
        }
        [Fact]
        public async Task Post_Terminate_JointVenture_Request_ShouldBe_Successful()
        {
            using var adminScope = fixture.AdminScope();
            var reason = new RejectRequestRequest() { Reason = "Testing termination" };
            var response2 = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{jointVentureTestRequest.Project.ProjectId}/requests/{jointVentureTestRequest.Request.Id}/terminate", reason);
            response2.Should().BeSuccessfull();
        }
        [Fact]
        public async Task Post_Terminate_Normal_Request_ShouldBe_Successful()
        {
            using var adminScope = fixture.AdminScope();
            var reason = new RejectRequestRequest() { Reason = "Testing termination" };
            var response2 = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{normalTestRequest.Project.ProjectId}/requests/{normalTestRequest.Request.Id}/terminate", reason);
            response2.Should().BeSuccessfull();
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

            public object WorkFlow { get; set; }
            public object ProvisioningStatus { get; set; }
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
