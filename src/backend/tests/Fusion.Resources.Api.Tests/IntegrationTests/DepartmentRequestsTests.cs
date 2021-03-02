using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class DepartmentRequestsTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {

        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;
        private FusionTestProjectBuilder testProject;
        private FusionTestResourceAllocationBuilder testRequest;

        private ApiPersonProfileV3 testUser;
        public DepartmentRequestsTests(ResourceApiFixture fixture, ITestOutputHelper output)
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
            testUser = PeopleServiceMock.AddTestProfile()
                .SaveProfile();

            // Mock project
            testProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .AddToMockService();

            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            testRequest = new FusionTestResourceAllocationBuilder()
               .WithOrgPositionId(testProject.Positions.First())
               .WithProject(testProject.Project)
               .WithProposedPerson(testUser)
               .WithAssignedDepartment("Test department");

            fixture.ContextResolver
               .AddContext(testProject.Project);

            var response = await adminClient.TestClientPostAsync($"/projects/{testRequest.Project.ProjectId}/requests", testRequest.Request, new { Id = Guid.Empty });
            response.Should().BeSuccessfull();
            testRequest.Request.Id = response.Value.Id;
        }

        #region GetDeparmentRequests

        [Fact]
        public async Task GetDepartmentRequests_ShouldIncludeRequest_WhenCurrentDepartment()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientGetAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/departments/{testRequest.Request.AssignedDepartment}/resources/requests");
            response.Should().BeSuccessfull();

            response.Value.Value.Should().OnlyContain(r => r.AssignedDepartment == "Test department");
        }

        [Fact]
        public async Task GetDepartmentRequests_ShouldNotIncludeRequests_WhenAssignedDepartmentEmpty()
        {
            using var adminScope = fixture.AdminScope();

            var unassignedRequest = new FusionTestResourceAllocationBuilder()
               .WithOrgPositionId(testProject.Positions.Skip(1).First())
               .WithProject(testProject.Project)
               .WithProposedPerson(testUser)
               .WithAssignedDepartment(null);
            ;
            var post = await Client.TestClientPostAsync($"/projects/{unassignedRequest.Project.ProjectId}/requests", unassignedRequest.Request, new { Id = Guid.Empty });
            post.Should().BeSuccessfull();
            var response = await Client.TestClientGetAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/departments/some department string/resources/requests");
            response.Should().BeSuccessfull();

            response.Value.Value.Should().HaveCount(0);
        }


        [Fact]
        public async Task GetDepartmentRequests_ShouldNotIncludeRequests_WhenAssignedDepartmentNotCurrentDepartment()
        {
            using var adminScope = fixture.AdminScope();

            var otherDepartmentRequest = new FusionTestResourceAllocationBuilder()
               .WithOrgPositionId(testProject.Positions.Skip(1).First())
               .WithProject(testProject.Project)
               .WithProposedPerson(testUser)
               .WithAssignedDepartment("Other department");
            ;
            var post = await Client.TestClientPostAsync($"/projects/{otherDepartmentRequest.Project.ProjectId}/requests", otherDepartmentRequest.Request, new { Id = Guid.Empty });
            post.Should().BeSuccessfull();
            var response = await Client.TestClientGetAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/departments/{testRequest.Request.AssignedDepartment}/resources/requests");
            response.Should().BeSuccessfull();

            response.Value.Value.Should().NotContain(r => r.AssignedDepartment == "Other department");
        }

        #endregion

        [Fact]
        public async Task GetDepartmentTimeline_IsSuccessfull()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientGetAsync<DepartmentRequestsWithTimelineTestModel>($"/departments/{testRequest.Request.AssignedDepartment}/resources/requests/timeline?timelineStart=2020-02-01T00:00:00Z&timelineEnd=2021-05-01T00:00:00Z");
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task GetDepartmentTimeline_WithoutTimelineStart_ShouldFail()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientGetAsync<DepartmentRequestsWithTimelineTestModel>($"/departments/{testRequest.Request.AssignedDepartment}/resources/requests/timeline?timelineEnd=2021-05-01T00:00:00Z");
            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task GetDepartmentTimeline_WithoutTimelineEndOrDuration_ShouldFail()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientGetAsync<DepartmentRequestsWithTimelineTestModel>($"/departments/{testRequest.Request.AssignedDepartment}/resources/requests/timeline?timelineStart=2021-05-01T00:00:00Z");
            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task GetDepartmentTimeline_CurrentDepartment_ShouldReturnCorrectResponse()
        {
            using var adminScope = fixture.AdminScope();
            var position = testProject.Positions.Skip(1).First();
            var newRequest = new FusionTestResourceAllocationBuilder()
              .WithOrgPositionId(position)
              .WithProject(testProject.Project)
              .WithProposedPerson(testUser)
              .WithAssignedDepartment("New department");

            var created = await Client.TestClientPostAsync($"/projects/{newRequest.Project.ProjectId}/requests", newRequest.Request, new { Id = Guid.Empty });
            created.Should().BeSuccessfull();

            var timelineStart = position.Instances.First().AppliesFrom.Date;
            var timelineEnd = position.Instances.First().AppliesTo.Date;
            var response = await Client.TestClientGetAsync<DepartmentRequestsWithTimelineTestModel>($"/departments/{newRequest.Request.AssignedDepartment}/resources/requests/timeline?timelineStart={timelineStart}&timelineEnd={timelineEnd}");
            response.Value.Requests.Should().OnlyContain(r => r.Id == created.Value.Id.ToString());
            response.Value.Timeline.Should().Contain(t => t.AppliesFrom == timelineStart && t.AppliesTo == timelineEnd && t.Items.Exists(i => i.Id == created.Value.Id.ToString()));
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
        public class DepartmentRequestsWithTimelineTestModel
        {
            public List<SimpleRequestTestModel>? Requests { get; set; }
            public List<DepartmentRequestsTimelineRangeTestModel>? Timeline { get; set; }

            public class SimpleRequestTestModel
            {
       
                public string Id { get; set; }
                public DateTime AppliesFrom { get; set; }
                public DateTime AppliesTo { get; set; }
                public double? Workload { get; set; }
                public string ProjectName { get; set; }
                public string PositionName { get; set; }
            }

            public class DepartmentRequestsTimelineRangeTestModel
            {
                public DateTime AppliesFrom { get; set; }
                public DateTime AppliesTo { get; set; }
                public List<RequestTimelineItemTestModel> Items { get; set; } = new List<RequestTimelineItemTestModel>();
                public double? Workload { get; set; } 
            }

            public class RequestTimelineItemTestModel
            {
                public string Id { get; set; } 
                public string PositionName { get; set; }
                public string ProjectName { get; set; }
                public double? Workload { get; set; }
            }
        }
    }
}
