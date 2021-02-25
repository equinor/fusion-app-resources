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
#nullable enable 

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class InternalResourceAllocationRequestTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;

        /// <summary>
        ///     Will be generated new for each test
        /// </summary>
        private ApiPersonProfileV3 testUser;


        // Created by the async lifetime
        private FusionTestResourceAllocationBuilder normalRequest = null!;
        private FusionTestResourceAllocationBuilder directRequest = null!;
        private FusionTestResourceAllocationBuilder jointVentureRequest = null!;
        private FusionTestProjectBuilder testProject = null!;

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
            testUser = PeopleServiceMock.AddTestProfile()
                .SaveProfile();

            // Mock project
            testProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .AddToMockService();

            // Prepare project with mocks
            normalRequest = new FusionTestResourceAllocationBuilder()
                    .WithRequestType(ApiAllocationRequestType.Normal)
                    .WithOrgPositionId(testProject.Positions.First())
                    .WithProposedPerson(testUser)
                    .WithIsDraft(true)
                    .WithProposedChanges(new ApiPropertiesCollection { { "PROPA", "CHANGEA" }, { "PROPB", "CHANGEB" } })
                    .WithProject(testProject.Project)
                ;

            directRequest = new FusionTestResourceAllocationBuilder()
                    .WithRequestType(ApiAllocationRequestType.Direct)
                    .WithOrgPositionId(testProject.Positions.Skip(1).First())
                    .WithProposedPerson(testUser)
                    .WithIsDraft(true)
                    .WithProposedChanges(new ApiPropertiesCollection { { "PROPA", "CHANGEA" }, { "PROPB", "CHANGEB" } })
                    .WithProject(testProject.Project)
                ;

            jointVentureRequest = new FusionTestResourceAllocationBuilder()
                    .WithRequestType(ApiAllocationRequestType.JointVenture)
                    .WithOrgPositionId(testProject.Positions.Skip(2).First())
                    .WithProposedPerson(testUser)
                    .WithIsDraft(true)
                    .WithProposedChanges(new ApiPropertiesCollection { { "PROPA", "CHANGEA" }, { "PROPB", "CHANGEB" } })
                    .WithProject(testProject.Project)
                ;

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(normalRequest.Project);

            // Prepare admin client
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            // Make sure we are able to create a request
            var response = await adminClient.TestClientPostAsync($"/projects/{normalRequest.Project.ProjectId}/requests", normalRequest.Request, new { Id = Guid.Empty });
            response.Should().BeSuccessfull();
            normalRequest.Request.Id = response.Value.Id;

            response = await adminClient.TestClientPostAsync($"/projects/{jointVentureRequest.Project.ProjectId}/requests", jointVentureRequest.Request, new { Id = Guid.Empty });
            response.Should().BeSuccessfull();
            jointVentureRequest.Request.Id = response.Value.Id;

            response = await adminClient.TestClientPostAsync($"/projects/{directRequest.Project.ProjectId}/requests", directRequest.Request, new { Id = Guid.Empty });
            response.Should().BeSuccessfull();
            directRequest.Request.Id = response.Value.Id;
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }

        #region delete tests
        [Fact]
        public async Task Delete_ProjectRequest_ShouldBeAuthorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientDeleteAsync($"/projects/{normalRequest.Project.ProjectId}/requests/{normalRequest.Request.Id}");
            response.Should().BeSuccessfull();
        }
        [Fact]
        public async Task Delete_ProjectRequest_NonExistingRequest_ShouldBeNotFound()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientDeleteAsync($"/projects/{normalRequest.Project.ProjectId}/requests/{Guid.NewGuid()}");
            response.Should().BeNotFound();
        }
        #endregion

        #region get tests
        [Fact]
        public async Task Get_ProjectRequest_ShouldBeAuthorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<ResourceAllocationRequestTestModel>($"/projects/{normalRequest.Project.ProjectId}/requests/{normalRequest.Request.Id}");
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, normalRequest.Request, adminScope);
        }
        [Fact]
        public async Task Get_ProjectRequests_ShouldBeAuthorized()
        {
            using var adminScope = fixture.AdminScope();

            var topResponseTest = await Client.TestClientGetAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/projects/{normalRequest.Project.ProjectId}/requests?$filter=assignedDepartment eq '{normalRequest.Request.AssignedDepartment}'");
            topResponseTest.Should().BeSuccessfull();
            topResponseTest.Value.Value.Count().Should().BeGreaterOrEqualTo(1);

            var response = await Client.TestClientGetAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/projects/{normalRequest.Project.ProjectId}/requests");
            response.Should().BeSuccessfull();

            response.Value.Value.Count().Should().BeGreaterOrEqualTo(3);

        }
        [Fact]
        public async Task Get_ProjectRequestsExpanded_ShouldBeAuthorized()
        {
            using var adminScope = fixture.AdminScope();

            var plainList = await Client.TestClientGetAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/projects/{normalRequest.Project.ProjectId}/requests");
            plainList.Should().BeSuccessfull();
            foreach (var m in plainList.Value.Value)
            {
                m.OrgPosition.Should().BeNull();
            }

            var expandedList = await Client.TestClientGetAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/projects/{normalRequest.Project.ProjectId}/requests?$expand=orgPosition");

            expandedList.Should().BeSuccessfull();
            foreach (var m in expandedList.Value.Value)
            {
                m.OrgPosition.Should().NotBeNull();
            }

        }
        [Fact]
        public async Task Get_InternalRequest_ShouldBeAuthorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<ResourceAllocationRequestTestModel>($"/resources/requests/internal/{normalRequest.Request.Id}");
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, normalRequest.Request, adminScope);
        }
        [Fact]
        public async Task Get_InternalRequests_ShouldBeAuthorized()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/resources/requests/internal");
            response.Should().BeSuccessfull();

            response.Value.Value.Count().Should().BeGreaterThan(0);

        }
        #endregion

        #region put tests
        [Fact]
        public async Task Put_ProjectRequest_ShouldBeAuthorized()
        {
            using var adminScope = fixture.AdminScope();
            var beforeUpdate = DateTimeOffset.UtcNow;
            var dict = new Dictionary<string, object> { { "orgpositioninstance.workload", 50 } };
            var updateRequest = new UpdateResourceAllocationRequest
            {
                ProjectId = normalRequest.Project.ProjectId,
                OrgPositionId = normalRequest.Request.OrgPositionId,
                OrgPositionInstance = normalRequest.Request.OrgPositionInstance,
                AssignedDepartment = "TPD",
                Discipline = "upd",
                IsDraft = false,
                AdditionalNote = "upd",
                ProposedPersonAzureUniqueId = normalRequest.Request.ProposedPersonAzureUniqueId,
                ProposedChanges = new ApiPropertiesCollection(dict)
            };

            var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/projects/{normalRequest.Project.ProjectId}/requests/{normalRequest.Request.Id}", updateRequest);
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, updateRequest, adminScope);
            response.Value.Updated?.Should().BeAfter(beforeUpdate);
        }
        [Fact]
        public async Task Put_ProjectRequest_InvalidRequest_ShouldBeUnsuccessful()
        {
            using var adminScope = fixture.AdminScope();

            var updateRequest = new UpdateResourceAllocationRequest { ProposedPersonAzureUniqueId = Guid.Empty };
            var response = await Client.TestClientPutAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/projects/{normalRequest.Project.ProjectId}/requests/{normalRequest.Request.Id}", updateRequest);
            response.Should().BeBadRequest("Invalid arguments passed");

        }
        [Fact]
        public async Task Put_InternalRequest_ShouldBeAuthorized()
        {
            using var adminScope = fixture.AdminScope();
            var beforeUpdate = DateTimeOffset.UtcNow;

            var updateRequest = new UpdateResourceAllocationRequest
            {
                ProjectId = normalRequest.Project.ProjectId,
                OrgPositionId = normalRequest.Request.OrgPositionId,
                OrgPositionInstance = normalRequest.Request.OrgPositionInstance,
                Discipline = "upd",
                IsDraft = false,
                AdditionalNote = "upd",
                ProposedPersonAzureUniqueId = normalRequest.Request.ProposedPersonAzureUniqueId
            };

            var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/resources/requests/internal/{normalRequest.Request.Id}", updateRequest);
            response.Should().BeSuccessfull();

            AssertPropsAreEqual(response.Value, updateRequest, adminScope);
            response.Value.Updated?.Should().BeAfter(beforeUpdate);
        }
        [Fact]
        public async Task Put_InternalRequest_EmptyRequest_ShouldNotModifyDbEntity()
        {
            using var adminScope = fixture.AdminScope();
            var updateRequest = new UpdateResourceAllocationRequest();
            var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/resources/requests/internal/{normalRequest.Request.Id}", updateRequest);
            response.Value.Updated.Should().BeNull();
        }
        #endregion

        #region post tests
        [Fact]
        public async Task Post_ProjectRequest_InvalidArguments_ShouldBeBadRequest()
        {
            normalRequest.Request.OrgPositionId = Guid.Empty;

            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientPostAsync($"/projects/{normalRequest.Project.ProjectId}/requests", normalRequest.Request, new { Id = Guid.Empty });

            response.Should().BeBadRequest();
        }
        [Fact]
        public async Task Post_ProjectRequest_InvalidRequest_ShouldBeUnsuccessful()
        {
            using var adminScope = fixture.AdminScope();
            var updateRequest = new CreateResourceAllocationRequest();
            var response = await Client.TestClientPostAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/projects/{normalRequest.Project.ProjectId}/requests", updateRequest);
            response.Should().BeBadRequest("Invalid arguments passed");

        }
        [Fact]
        public async Task Post_ProjectRequest_NormalApprovalSteps_ShouldBeAssigned()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{normalRequest.Project.ProjectId}/requests/{normalRequest.Request.Id}/approve", null);
            response.Value.State.Should().Be("Proposed");
            response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{normalRequest.Project.ProjectId}/requests/{normalRequest.Request.Id}/approve", null);
            response.Value.State.Should().Be("Accepted");
        }
        [Fact]
        public async Task Post_ProjectRequest_JointVentureApprovalSteps_ShouldBeAssigned()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{jointVentureRequest.Project.ProjectId}/requests/{jointVentureRequest.Request.Id}/approve", null);
            response.Value.State.Should().Be("Accepted");
        }
        [Fact]
        public async Task Post_ProjectRequest_DirectApprovalSteps_ShouldBeAssigned()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/projects/{directRequest.Project.ProjectId}/requests/{directRequest.Request.Id}/approve", null);
            response.Value.State.Should().Be("Accepted");
        }
        [Fact]
        public async Task Post_ProjectRequest_DirectApproval_ShouldBeProvisioned()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPostAsync<ResourceAllocationRequestTestModel>($"/resources/requests/internal/{directRequest.Request.Id}/provision", null);
            response.Should().BeSuccessfull();
            response.Value.ProvisioningStatus.State.Should().Be("Provisioned");
        }

        #endregion

        #region test helpers
        private static void AssertPropsAreEqual(ResourceAllocationRequestTestModel response, CreateResourceAllocationRequest request, TestClientScope scope)
        {
            $"{request.Type}".Should().Be(response.Type);
            request.AssignedDepartment?.Should().Be(response.AssignedDepartment);
            request.Discipline?.Should().Be(response.Discipline);

            request.ProjectId?.Should().Be(response.Project!.Id);
            request.OrgPositionId?.Should().Be(response.OrgPosition!.Id);
            request.OrgPositionInstance?.Id.Should().Be(response.OrgPositionInstance!.Id);
            request.ProposedPersonAzureUniqueId?.Should().Be(response.ProposedPerson!.AzureUniquePersonId);
            request.AdditionalNote?.Should().Be(response.AdditionalNote);
            request.IsDraft?.Should().Be(response.IsDraft.GetValueOrDefault());
            if (request.ProposedChanges != null)
            {
                foreach (var (key, value) in request.ProposedChanges)
                {
                    var item = response.ProposedChanges?.First(x => string.Equals(x.Key, key, StringComparison.InvariantCultureIgnoreCase));
                    item?.Value.Should().Be(value);
                }
            }

            response.CreatedBy.AzureUniquePersonId.Should().Be(scope.Profile.AzureUniqueId!.Value);
            response.LastActivity.Should().NotBeNull();
            response.Workflow.State.Should().NotBeNullOrEmpty();
            response.State.Should().NotBeNullOrEmpty();

        }
        private static void AssertPropsAreEqual(ResourceAllocationRequestTestModel response, UpdateResourceAllocationRequest request, TestClientScope scope)
        {
            request.AssignedDepartment?.Should().Be(response.AssignedDepartment);
            request.Discipline?.Should().Be(response.Discipline);

            request.ProjectId?.Should().Be(response.Project!.Id);
            request.OrgPositionId?.Should().Be(response.OrgPosition!.Id);
            request.OrgPositionInstance?.Id.Should().Be(response.OrgPositionInstance!.Id);
            request.ProposedPersonAzureUniqueId?.Should().Be(response.ProposedPerson!.AzureUniquePersonId);
            request.AdditionalNote?.Should().Be(response.AdditionalNote);
            request.IsDraft?.Should().Be(response.IsDraft.GetValueOrDefault());
            if (request.ProposedChanges != null)
            {
                foreach (var (key, value) in request.ProposedChanges)
                {
                    var item = response.ProposedChanges?.First(x => string.Equals(x.Key, key, StringComparison.InvariantCultureIgnoreCase));
                    item?.Value.Should().Be(value);
                }
            }

            response.CreatedBy.AzureUniquePersonId.Should().Be(scope.Profile.AzureUniqueId!.Value);
            response.LastActivity.Should().NotBeNull();
            response.Workflow.State.Should().NotBeNullOrEmpty();
            response.State.Should().NotBeNullOrEmpty();
        }
        #endregion
    }

    #region test models
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
        public string? State { get; set; }
        public string? AssignedDepartment { get; set; }
        public string? Discipline { get; set; }
        public ObjectWithId? Project { get; set; }
        public string? Type { get; set; }
        public ObjectWithId OrgPosition { get; set; } = null!;
        public ObjectWithId OrgPositionInstance { get; set; } = null!;
        public string? AdditionalNote { get; set; }
        public bool? IsDraft { get; set; }
        public Dictionary<string, object>? ProposedChanges { get; set; }
        public ObjectWithAzureUniquePerson? ProposedPerson { get; set; }

        public ObjectWithAzureUniquePerson CreatedBy { get; set; } = null!;
        public ObjectWithAzureUniquePerson? UpdatedBy { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public DateTimeOffset? LastActivity { get; set; }

        public ObjectWithState Workflow { get; set; } = null!;
        public ObjectWithState ProvisioningStatus { get; set; } = null!;

    }
    public class ObjectWithAzureUniquePerson
    {
        public Guid AzureUniquePersonId { get; set; }
    }

    public class ObjectWithId
    {
        public Guid Id { get; set; }
    }
    public class ObjectWithState
    {
        public string? State { get; set; }
    }
    #endregion
}