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
        private TestApiInternalRequestModel testRequest;

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
        private const string ApiVersion = "api-version=1.0-Preview";

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

            fixture.ContextResolver
               .AddContext(testProject.Project);

            testRequest = await adminClient.CreateDefaultRequestAsync(testProject);
            testRequest = await adminClient.AssignAnDepartmentAsync(testRequest.Id);
        }

        #region GetDeparmentRequests

        [Fact]
        public async Task GetDepartmentRequests_ShouldIncludeRequest_WhenCurrentDepartment()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(
                $"/departments/{testRequest.AssignedDepartment}/resources/requests?api-version=1.0-preview");
            response.Should().BeSuccessfull();

            response.Value.Value.Should().OnlyContain(r => r.AssignedDepartment == testRequest.AssignedDepartment);
        }

        [Fact]
        public async Task GetDepartmentRequests_ShouldNotReturn_UnassignedRequest()
        {
            using var adminScope = fixture.AdminScope();

            var unassignedRequest = await Client.CreateDefaultRequestAsync(testProject);

            var response = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(
                $"/departments/{testRequest.AssignedDepartment}/resources/requests?api-version=1.0-preview");
            response.Should().BeSuccessfull();

            response.Value.Value.Should().NotContain(r => r.Id == unassignedRequest.Id);
        }


        [Fact]
        public async Task GetDepartmentRequests_ShouldNotIncludeRequests_WhenAssignedToOtherDepartment()
        {
            using var adminScope = fixture.AdminScope();

            var otherDepartment = InternalRequestData.PickRandomDepartment(testRequest.AssignedDepartment);

            var otherDepartmentRequest = await Client.CreateDefaultRequestAsync(testProject);
            await Client.AssignDepartmentAsync(otherDepartmentRequest.Id, otherDepartment);

            var response = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(
                $"/departments/{testRequest.AssignedDepartment}/resources/requests?{ApiVersion}");
            response.Should().BeSuccessfull();

            response.Value.Value.Should().NotContain(r => r.AssignedDepartment == otherDepartment);
        }

        #endregion

        [Fact]
        public async Task GetDepartmentTimeline_IsSuccessfull()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientGetAsync<TestApiDepartmentRequests>(
                $"/departments/{testRequest.AssignedDepartment}/resources/requests/timeline?{ApiVersion}&timelineStart=2020-02-01T00:00:00Z&timelineEnd=2021-05-01T00:00:00Z");
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task GetDepartmentTimeline_WithoutTimelineStart_ShouldFail()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientGetAsync<TestApiDepartmentRequests>($"/departments/{testRequest.AssignedDepartment}/resources/requests/timeline?{ApiVersion}&timelineEnd=2021-05-01T00:00:00Z");
            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task GetDepartmentTimeline_WithoutTimelineEndOrDuration_ShouldFail()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientGetAsync<TestApiDepartmentRequests>($"/departments/{testRequest.AssignedDepartment}/resources/requests/timeline?{ApiVersion}&timelineStart=2021-05-01T00:00:00Z");
            response.Should().BeBadRequest();
        }

        //[Fact]
        //public async Task GetDepartmentTimeline_CurrentDepartment_ShouldReturnCorrectResponse()
        //{
        //    using var adminScope = fixture.AdminScope();

            


        //    var position = testProject.AddPosition();
        //    var request = await Client.CreateRequestAsync(testProject.Project.ProjectId, r => r.WithPosition(position));
        //    request = await Client.AssignDepartmentAsync(request.Id, testRequest.AssignedDepartment);

        //    //use generated dates from request to make sure it is within time range
        //    var timelineStart = DateTime.SpecifyKind(position.Instances.First().AppliesFrom.Date, DateTimeKind.Utc);
        //    var timelineEnd = DateTime.SpecifyKind(position.Instances.First().AppliesTo.Date, DateTimeKind.Utc);
        //    var response = await Client.TestClientGetAsync<TestApiDepartmentRequests>($"/departments/{request.AssignedDepartment}/resources/requests/timeline?{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}");
        //    response.Value.Requests.Should().Contain(r => r.Id == $"{request.Id}");
        //    response.Value.Timeline.Should().Contain(t => t.AppliesFrom == timelineStart && t.AppliesTo == timelineEnd && t.Items.Exists(i => i.Id == created.Value.Id.ToString()));
        //}

        //[Fact]
        //public async Task GetDepartmentTimeline_ShouldNotReturnRequest_WhenAssignedDepartmentNotCurrentDepartment() {
        //    using var adminScope = fixture.AdminScope();
        //    var position = testProject.Positions.Skip(1).First();
        //    var newRequest = new FusionTestResourceAllocationBuilder()
        //      .WithOrgPositionId(position)
        //      .WithProject(testProject.Project)
        //      .WithProposedPerson(testUser)
        //      .WithAssignedDepartment("Other department");

        //    var created = await Client.TestClientPostAsync($"/projects/{newRequest.Project.ProjectId}/requests", newRequest.Request, new { Id = Guid.Empty });
        //    created.Should().BeSuccessfull();

        //    //use generated dates from request to make sure it is within time range
        //    var timelineStart = DateTime.SpecifyKind(position.Instances.First().AppliesFrom.Date, DateTimeKind.Utc);
        //    var timelineEnd = DateTime.SpecifyKind(position.Instances.First().AppliesTo.Date, DateTimeKind.Utc);
        //    var response = await Client.TestClientGetAsync<DepartmentRequestsWithTimelineTestModel>($"/departments/{testRequest.Request.AssignedDepartment}/resources/requests/timeline?{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}");
        //    response.Value.Requests.Should().NotContain(r => r.Id == created.Value.Id.ToString());
        //}

        //[Fact]
        //public async Task GetDepartmentTimeline_ShouldNotReturn_UnassignedRequest()
        //{
        //    using var adminScope = fixture.AdminScope();
        //    var position = testProject.Positions.Skip(1).First();
        //    var unassignedRequest = new FusionTestResourceAllocationBuilder()
        //      .WithOrgPositionId(position)
        //      .WithProject(testProject.Project)
        //      .WithProposedPerson(testUser)
        //      .WithAssignedDepartment(null);

        //    var created = await Client.TestClientPostAsync($"/projects/{unassignedRequest.Project.ProjectId}/requests", unassignedRequest.Request, new { Id = Guid.Empty });
        //    created.Should().BeSuccessfull();

        //    //use generated dates from request to make sure it is within time range
        //    var timelineStart = DateTime.SpecifyKind(position.Instances.First().AppliesFrom.Date, DateTimeKind.Utc);
        //    var timelineEnd = DateTime.SpecifyKind(position.Instances.First().AppliesTo.Date, DateTimeKind.Utc);
        //    var response = await Client.TestClientGetAsync<DepartmentRequestsWithTimelineTestModel>($"/departments/{testRequest.Request.AssignedDepartment}/resources/requests/timeline?{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}");
        //    response.Value.Requests.Should().NotContain(r => r.Id == created.Value.Id.ToString());
        //}

        //[Fact]
        //public async Task GetDepartmentTimeline_ShouldNotReturn_RequestNotInTimeRange()
        //{
        //    using var adminScope = fixture.AdminScope();
        //    var position = testProject.Positions.Skip(1).First();
        //    var unassignedRequest = new FusionTestResourceAllocationBuilder()
        //      .WithOrgPositionId(position)
        //      .WithProject(testProject.Project)
        //      .WithProposedPerson(testUser)
        //      .WithAssignedDepartment("Test department");

        //    var created = await Client.TestClientPostAsync($"/projects/{unassignedRequest.Project.ProjectId}/requests", unassignedRequest.Request, new { Id = Guid.Empty });
        //    created.Should().BeSuccessfull();

        //    //use generated dates from request to make sure it is NOT within time range
        //    var timelineStart = DateTime.SpecifyKind(position.Instances.First().AppliesTo.Date.AddDays(1), DateTimeKind.Utc);
        //    var timelineEnd = DateTime.SpecifyKind(timelineStart.Date.AddDays(10), DateTimeKind.Utc);
        //    var response = await Client.TestClientGetAsync<DepartmentRequestsWithTimelineTestModel>($"/departments/{testRequest.Request.AssignedDepartment}/resources/requests/timeline?{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}");
        //    response.Value.Requests.Should().NotContain(r => r.Id == created.Value.Id.ToString());
        //}

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }

        public class TestApiDepartmentRequests
        {
            public List<SimpleRequestTestModel> Requests { get; set; }
            public List<DepartmentRequestsTimelineRangeTestModel> Timeline { get; set; }

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
