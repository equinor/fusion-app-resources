using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Fusion.Services.LineOrg.ApiModels;
using Newtonsoft.Json.Linq;
using Fusion.ApiClients.Org;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    [Collection("Integration")]
    public class DepartmentRequestsTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        const string TimelineDepartment = "TPD TST TIL DPT3";
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;
        private FusionTestProjectBuilder testProject;
        private TestApiInternalRequestModel testRequest;

        private ApiOrgUnit userOrgUnit;
        private ApiOrgUnit assignedOrgUnit;
        private ApiPersonProfileV3 user;
        public DepartmentRequestsTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);
        }

        private HttpClient Client => fixture.ApiFactory.CreateClient();
        private const string ApiVersion = "api-version=1.0";

        public async Task InitializeAsync()
        {
            user = fixture.AddResourceOwner(TimelineDepartment);

            userOrgUnit = fixture.AddOrgUnit(user.FullDepartment);

            // Mock project
            testProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .WithProperty("pimsWriteSyncEnabled", true)
                .AddToMockService();

            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            fixture.ContextResolver
               .AddContext(testProject.Project);

            testRequest = await adminClient.CreateDefaultRequestAsync(testProject);
            testRequest = await adminClient.StartProjectRequestAsync(testProject, testRequest.Id);
            testRequest = await adminClient.AssignRandomDepartmentAsync(testRequest.Id);

            // Should either create or fetch existing org unit.
            assignedOrgUnit = fixture.AddOrgUnit(testRequest.AssignedDepartment);

        }

        #region GetDeparmentRequests

        [Fact]
        public async Task GetDepartmentRequests_ShouldIncludeRequest_WhenCurrentDepartment()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(
                $"/departments/{testRequest.AssignedDepartment}/resources/requests");
            response.Should().BeSuccessfull();

            response.Value.Value.Should().OnlyContain(r => r.AssignedDepartment == testRequest.AssignedDepartment);
        }

        [Fact]
        public async Task GetDepartmentRequests_ShouldNotReturn_UnassignedRequest()
        {
            using var adminScope = fixture.AdminScope();

            var unassignedRequest = await Client.CreateDefaultRequestAsync(testProject);

            var response = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(
                $"/departments/{testRequest.AssignedDepartment}/resources/requests");
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


        [Fact]
        public async Task GetDepartmentRequests_ShouldIncludeNameOnProposedPerson()
        {
            using var adminScope = fixture.AdminScope();
            var proposedPerson = fixture.AddProfile(FusionAccountType.Employee);
            await Client.ProposePersonAsync(testRequest.Id, proposedPerson);

            var response = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(
                $"/departments/{testRequest.AssignedDepartment}/resources/requests");

            response.Value.Value.Should().OnlyContain(req => !String.IsNullOrEmpty(req.ProposedPerson.Person.Name));
        }

        [Fact]
        public async Task GetDepartmentRequests_ShouldSupportSapId()
        {
            using var adminScope = fixture.AdminScope();
            var proposedPerson = fixture.AddProfile(FusionAccountType.Employee);
            await Client.ProposePersonAsync(testRequest.Id, proposedPerson);

            var response = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(
                $"/departments/{assignedOrgUnit.SapId}/resources/requests");

            response.Value.Value.Should().Contain(r => r.Id == testRequest.Id);
        }

        [Fact]
        public async Task GetDepartmentRequests_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
        {
            using var adminScope = fixture.AdminScope();
            var proposedPerson = fixture.AddProfile(FusionAccountType.Employee);
            await Client.ProposePersonAsync(testRequest.Id, proposedPerson);

            var assignedOrgUnit = fixture.AddOrgUnit(testRequest.AssignedDepartment); // Should either create or fetch existing org unit.

            var response = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(
                $"/departments/12312312399/resources/requests");

            response.Should().BeNotFound();
        }

        [Fact]
        public async Task GetDepartmentRequests_ProjectsShouldHaveState()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(
                $"/departments/{assignedOrgUnit.SapId}/resources/requests");
            response.Should().BeSuccessfull();
            response.Value.Value.All(request => request.Project.State.Length > 0).Should().BeTrue();
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

        //[Fact]
        //public async Task GetRequestTimelineShouldNotHaveSegmentsWithOverlappingDates()
        //{
        //    string department = InternalRequestData.RandomDepartment;

        //    using var adminScope = fixture.AdminScope();

        //    var rq1 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
        //        .WithInstances(i => i.AddInstance(new DateTime(2021, 03, 09), TimeSpan.FromDays(6))));
        //    await Client.StartProjectRequestAsync(testProject, rq1.Id);
        //    await Client.AssignDepartmentAsync(rq1.Id, department);

        //    var rq2 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
        //        .WithInstances(i => i.AddInstance(new DateTime(2021, 03, 15), TimeSpan.FromDays(6))));
        //    await Client.StartProjectRequestAsync(testProject, rq2.Id);
        //    await Client.AssignDepartmentAsync(rq2.Id, department);

        //    var timelineStart = new DateTime(2021, 03, 01);
        //    var timelineEnd = new DateTime(2021, 03, 31);

        //    var response = await Client.TestClientGetAsync<TestApiDepartmentRequests>($"/departments/{department}/resources/requests/timeline?{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}");

        //    var previousEnd = (DateTime?)null;
        //    response.Value.Requests.Should().NotBeEmpty();
        //    foreach (var segment in response.Value.Timeline.OrderBy(s => s.AppliesFrom))
        //    {
        //        if (previousEnd.HasValue)
        //        {
        //            segment.AppliesFrom.Should().NotBe(previousEnd.Value);
        //        }

        //        previousEnd = segment.AppliesTo;
        //    }
        //}


        [Fact]
        public async Task GetRequestTimelineShouldSegmentRequests()
        {
            string department = TimelineDepartment;

            using var adminScope = fixture.AdminScope();

            var rq1 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2022, 03, 09), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq1.Id);
            await Client.AssignDepartmentAsync(rq1.Id, department);

            var rq2 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2022, 03, 15), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq2.Id);
            await Client.AssignDepartmentAsync(rq2.Id, department);

            var rq3 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2022, 03, 23), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq3.Id);
            await Client.AssignDepartmentAsync(rq3.Id, department);

            var timelineStart = new DateTime(2022, 03, 01);
            var timelineEnd = new DateTime(2022, 03, 31);

            var response = await Client.TestClientGetAsync<TestApiDepartmentRequests>($"/departments/{department}/resources/requests/timeline?{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}");

            var segments = response.Value.Timeline.OrderBy(s => s.AppliesFrom).ToList();

            segments[0].Items.Should().HaveCount(1);
            segments[0].AppliesFrom.Date.Should().Be(new DateTime(2022, 03, 09));
            segments[0].AppliesTo.Date.Should().Be(new DateTime(2022, 03, 14));

            segments[1].Items.Should().HaveCount(2);
            segments[1].AppliesFrom.Date.Should().Be(new DateTime(2022, 03, 15));
            segments[1].AppliesTo.Date.Should().Be(new DateTime(2022, 03, 15));

            segments[2].Items.Should().HaveCount(1);
            segments[2].AppliesFrom.Date.Should().Be(new DateTime(2022, 03, 16));
            segments[2].AppliesTo.Date.Should().Be(new DateTime(2022, 03, 21));

            segments[3].Items.Should().HaveCount(1);
            segments[3].AppliesFrom.Date.Should().Be(new DateTime(2022, 03, 23));
            segments[3].AppliesTo.Date.Should().Be(new DateTime(2022, 03, 29));
        }


        [Fact]
        public async Task GetRequestTimelineShouldTruncateSegmentsToFromToDate()
        {
            string department = TimelineDepartment;

            using var adminScope = fixture.AdminScope();

            var rq1 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2023, 02, 27), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq1.Id);
            await Client.AssignDepartmentAsync(rq1.Id, department);

            var rq2 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2023, 03, 15), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq2.Id);
            await Client.AssignDepartmentAsync(rq2.Id, department);

            var rq3 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2023, 03, 23), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq3.Id);
            await Client.AssignDepartmentAsync(rq3.Id, department);

            var timelineStart = new DateTime(2023, 03, 01);
            var timelineEnd = new DateTime(2023, 03, 31);

            var response = await Client.TestClientGetAsync<TestApiDepartmentRequests>($"/departments/{department}/resources/requests/timeline?{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}");

            var segments = response.Value.Timeline.OrderBy(s => s.AppliesFrom).ToList();

            segments[0].Items.Should().HaveCount(1);
            segments[0].AppliesFrom.Date.Should().Be(new DateTime(2023, 03, 01));
            segments[0].AppliesTo.Date.Should().Be(new DateTime(2023, 03, 05));

            segments[1].Items.Should().HaveCount(1);
            segments[1].AppliesFrom.Date.Should().Be(new DateTime(2023, 03, 15));
            segments[1].AppliesTo.Date.Should().Be(new DateTime(2023, 03, 21));

            segments[2].Items.Should().HaveCount(1);
            segments[2].AppliesFrom.Date.Should().Be(new DateTime(2023, 03, 23));
            segments[2].AppliesTo.Date.Should().Be(new DateTime(2023, 03, 29));
        }

        [Fact]
        public async Task GetRequestTimelineShouldDisregardTimeOfDay()
        {
            string department = TimelineDepartment;

            using var adminScope = fixture.AdminScope();

            var rq1 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2019, 02, 27, 09, 43, 43), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq1.Id);
            await Client.AssignDepartmentAsync(rq1.Id, department);

            var rq2 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2019, 03, 15), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq2.Id);
            await Client.AssignDepartmentAsync(rq2.Id, department);

            var rq3 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2019, 03, 23, 14, 44, 58), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq3.Id);
            await Client.AssignDepartmentAsync(rq3.Id, department);

            var timelineStart = new DateTime(2019, 03, 01);
            var timelineEnd = new DateTime(2019, 03, 31);

            var response = await Client.TestClientGetAsync<TestApiDepartmentRequests>($"/departments/{department}/resources/requests/timeline?{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}");

            var segments = response.Value.Timeline.OrderBy(s => s.AppliesFrom).ToList();

            segments[0].Items.Should().HaveCount(1);
            segments[0].AppliesFrom.Should().Be(new DateTime(2019, 03, 01));
            segments[0].AppliesTo.Should().Be(new DateTime(2019, 03, 05));

            segments[1].Items.Should().HaveCount(1);
            segments[1].AppliesFrom.Should().Be(new DateTime(2019, 03, 15));
            segments[1].AppliesTo.Should().Be(new DateTime(2019, 03, 21));

            segments[2].Items.Should().HaveCount(1);
            segments[2].AppliesFrom.Should().Be(new DateTime(2019, 03, 23));
            segments[2].AppliesTo.Should().Be(new DateTime(2019, 03, 29));
        }

        [Fact]
        public async Task GetRequestTimelineShouldNotHaveGaps()
        {
            string department = TimelineDepartment;

            using var adminScope = fixture.AdminScope();

            var rq1 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2020, 03, 01), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq1.Id);
            await Client.AssignDepartmentAsync(rq1.Id, department);

            var rq2 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2020, 03, 02), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq2.Id);
            await Client.AssignDepartmentAsync(rq2.Id, department);

            var rq3 = await Client.CreateDefaultRequestAsync(testProject, null, p => p
                .WithInstances(i => i.AddInstance(new DateTime(2020, 03, 03), TimeSpan.FromDays(6))));
            await Client.StartProjectRequestAsync(testProject, rq3.Id);
            await Client.AssignDepartmentAsync(rq3.Id, department);

            var timelineStart = new DateTime(2020, 03, 01);
            var timelineEnd = new DateTime(2020, 03, 31);

            var response = await Client.TestClientGetAsync<TestApiDepartmentRequests>($"/departments/{department}/resources/requests/timeline?{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}");

            var segments = response.Value.Timeline.OrderBy(s => s.AppliesFrom).ToList();

            segments[0].Items.Should().HaveCount(1);
            segments[0].AppliesFrom.Should().Be(new DateTime(2020, 03, 01));
            segments[0].AppliesTo.Should().Be(new DateTime(2020, 03, 01));

            segments[1].Items.Should().HaveCount(2);
            segments[1].AppliesFrom.Should().Be(new DateTime(2020, 03, 02));
            segments[1].AppliesTo.Should().Be(new DateTime(2020, 03, 02));

            segments[2].Items.Should().HaveCount(3);
            segments[2].AppliesFrom.Should().Be(new DateTime(2020, 03, 03));
            segments[2].AppliesTo.Should().Be(new DateTime(2020, 03, 07));

            segments[3].Items.Should().HaveCount(2);
            segments[3].AppliesFrom.Should().Be(new DateTime(2020, 03, 08));
            segments[3].AppliesTo.Should().Be(new DateTime(2020, 03, 08));

            segments[4].Items.Should().HaveCount(1);
            segments[4].AppliesFrom.Should().Be(new DateTime(2020, 03, 09));
            segments[4].AppliesTo.Should().Be(new DateTime(2020, 03, 09));
        }

        [Fact]
        public async Task GetTimeline_ShouldIncludeTaskDetails()
        {
            using var adminScope = fixture.AdminScope();

            var absenceResp = await Client.AddAbsence(user, x =>
            {
                x.AppliesFrom = new DateTime(2020, 03, 01);
                x.AppliesTo = new DateTime(2020, 04, 15);
                x.TaskDetails = new TestTaskDetails()
                {
                    RoleName = "TestRole",
                    Location = "Norway",
                };
            });
            var absence = absenceResp.Value;

            var timelineStart = new DateTime(2020, 03, 01);
            var timelineEnd = new DateTime(2020, 03, 31);


            var response = await Client.TestClientGetAsync<TestResponse>(
                $"/departments/{user.Department}/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}"
            );

            var person = response.Value.value
                .FirstOrDefault(x => x.azureUniquePersonId == user.AzureUniqueId);
            person.employmentStatuses.Should().Contain(x => x.id == absence.Id);

            var timeline = person.timeline.Single();
            timeline.items.Should().Contain(x => x.id == absence.Id.ToString());
        }

        [Fact]
        public async Task GetTimeline_ShouldSupportSAPId()
        {
            using var adminScope = fixture.AdminScope();

            var timelineStart = new DateTime(2020, 03, 01);
            var timelineEnd = new DateTime(2020, 03, 31);

            var response = await Client.TestClientGetAsync<TestResponse>(
                $"/departments/{userOrgUnit.SapId}/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}"
            );

            var person = response.Value.value
                .FirstOrDefault(x => x.azureUniquePersonId == user.AzureUniqueId);
            person.Should().NotBeNull("User should exist in the department");
        }

        [Fact]
        public async Task GetTimeline_ShouldReturnNotFound_WhenSapIdDoesNotExist()
        {
            using var adminScope = fixture.AdminScope();

            var timelineStart = new DateTime(2020, 03, 01);
            var timelineEnd = new DateTime(2020, 03, 31);

            var response = await Client.TestClientGetAsync<TestResponse>(
                $"/departments/9999999999999/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}"
            );

            response.Should().BeNotFound();
        }

        [Fact]
        public async Task GetTimeline_ShouldNotIncludeTaskDetailsForOtherResourceOwner()
        {
            TestAbsence absence;
            using (var adminScope = fixture.AdminScope())
            {
                var absenceResp = await Client.AddAbsence(user, x =>
                {
                    x.AppliesFrom = new DateTime(2020, 03, 01);
                    x.AppliesTo = new DateTime(2020, 04, 15);
                    x.IsPrivate = true;
                    x.TaskDetails = new TestTaskDetails()
                    {
                        RoleName = "TestRole",
                        Location = "Norway",
                    };
                });
                absenceResp.Should().BeSuccessfull();
                absence = absenceResp.Value;
            }

            var resourceOwner = fixture.AddResourceOwner("TPD TST QWE");

            using (var userScope = fixture.UserScope(resourceOwner))
            {
                var timelineStart = new DateTime(2020, 03, 01);
                var timelineEnd = new DateTime(2020, 03, 31);

                var response = await Client.TestClientGetAsync<TestResponse>(
                    $"/departments/{TimelineDepartment}/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}"
                );

                var person = response.Value.value
                    .FirstOrDefault(x => x.azureUniquePersonId == user.AzureUniqueId);
                var actualAbsence = person.employmentStatuses.FirstOrDefault(x => x.id == absence.Id);

                actualAbsence.taskDetails.isHidden.Should().Be(true);
                actualAbsence.taskDetails.roleName.Should().NotBe(absence.TaskDetails.RoleName);
                actualAbsence.taskDetails.taskName.Should().NotBe(absence.TaskDetails.TaskName);

                var timeline = person.timeline.Single();
                var absenceTimelineItem = timeline.items.FirstOrDefault(x => x.id == absence.Id.ToString());

                absenceTimelineItem.roleName.Should().NotBe(absence.TaskDetails.RoleName);
                absenceTimelineItem.taskName.Should().NotBe(absence.TaskDetails.TaskName);
            }
        }

        [Fact]
        public async Task GetTimelineShouldNotIncludePositionsOutsideTimeframe()
        {
            const string department = "PDP BTAD AWQ";
            fixture.AddOrgUnit(department);

            var project = new FusionTestProjectBuilder()
                .WithPositions(10, 50)
                .AddToMockService();

            var profile = fixture.AddProfile(s =>
            {
                s.WithFullDepartment(department);
                s.WithPositions(project.Positions);
            });

            using var scope = fixture.AdminScope();
            var timelineStart = new DateTime(2020, 03, 01);
            var timelineEnd = new DateTime(2020, 03, 31);

            var response = await Client.TestClientGetAsync<TestResponse>(
                $"/departments/{department}/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}"
            );

            response.Should().BeSuccessfull();
            response.Value.value
                .SelectMany(x => x.positionInstances)
                .Any(x => x.AppliesTo < timelineStart || x.AppliesFrom > timelineEnd)
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task GetTimelineShouldNotIncludeEmploymentStatusesOutsideTimeframe()
        {
            using var adminScope = fixture.AdminScope();

            await Client.AddAbsence(user, x =>
                                          {
                                              x.AppliesFrom = new DateTime(2021, 08, 06);
                                              x.AppliesTo = new DateTime(2021, 09, 03);
                                              x.TaskDetails = new TestTaskDetails()
                                              {
                                                  RoleName = "TestRole",
                                                  Location = "Norway",
                                              };
                                          });

            var timelineStart = new DateTime(2022, 04, 01);
            var timelineEnd = new DateTime(2022, 09, 30);


            var response = await Client.TestClientGetAsync<TestResponse>(
                                                                         $"/departments/{user.Department}/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}"
                                                                        );

            response.Should().BeSuccessfull();
            response.Value.value
                .SelectMany(x => x.employmentStatuses)
                .Any(x => x.appliesTo < timelineStart || x.appliesFrom > timelineEnd)
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task GetTimelineShouldIncludeEmploymentStatusesWithAppliesToNull()
        {
            using var adminScope = fixture.AdminScope();

            await Client.AddAbsence(user, x =>
                                          {
                                              x.AppliesFrom = new DateTime(2021, 08, 06);
                                              x.AppliesTo = null;
                                          });

            var timelineStart = new DateTime(2022, 04, 01);
            var timelineEnd = new DateTime(2022, 09, 30);


            var response = await Client.TestClientGetAsync<TestResponse>(
                                                                         $"/departments/{user.Department}/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}"
                                                                        );

            response.Should().BeSuccessfull();
            response.Value.value
                    .SelectMany(x => x.employmentStatuses)
                    .Any(x => x.appliesTo == null)
                    .Should()
                    .BeTrue();
        }


        [Fact]
        public async Task GetTimeline_ShouldNotIncludeRequestsOutsideTimeframe()
        {
            TestApiInternalRequestModel request;
            using var adminScope = fixture.AdminScope();
            request = await Client.CreateDefaultRequestAsync(testProject, positionSetup: pos => pos.WithInstances(x =>
            {
                x.AddInstance(new DateTime(2021, 10, 01), TimeSpan.FromDays(30));
            }));
            await Client.AssignDepartmentAsync(request.Id, user.FullDepartment);
            await Client.ProposePersonAsync(request.Id, user);

            var timelineStart = new DateTime(2022, 04, 01);
            var timelineEnd = new DateTime(2022, 09, 30);

            var response = await Client.TestClientGetAsync<TestResponse>(
                $"/departments/{user.Department}/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}"
            );

            var result = response.Value.value.FirstOrDefault(x => x.azureUniquePersonId == user.AzureUniqueId);
            result.pendingRequests.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRequests_ShouldNotIncludeRequestsOutsideCurrentAllocations()
        {
            const string department = "PDP BTAD AWQ";
            fixture.AddOrgUnit(department);

            var project = new FusionTestProjectBuilder()
                          .WithPositions(10, 50)
                          .AddToMockService();

            var profile = fixture.AddProfile(s => s.WithFullDepartment(department).WithPositions(project.Positions));

            using var scope = fixture.AdminScope();

            var response = await Client.TestClientGetAsync<TestResponse>(
                                                                         $"/departments/{department}/resources/personnel/?includeCurrentAllocations=true&{ApiVersion}"
                                                                        );

            response.Should().BeSuccessfull();

            response.Value.value
                    .SelectMany(x => x.positionInstances)
                    .Any(x => x.AppliesFrom > DateTime.Now || x.AppliesTo < DateTime.Now)
                    .Should()
                    .BeFalse();
        }

        [Fact]
        public async Task GetTimeline_ShouldIncludeRequests()
        {
            TestApiInternalRequestModel request;
            using var adminScope = fixture.AdminScope();
            request = await Client.CreateDefaultRequestAsync(testProject, positionSetup: pos => pos.WithInstances(x =>
            {
                x.AddInstance(new DateTime(2020, 03, 02), TimeSpan.FromDays(15));
            }));
            await Client.AssignDepartmentAsync(request.Id, user.FullDepartment);
            await Client.ProposePersonAsync(request.Id, user);

            var timelineStart = new DateTime(2020, 03, 01);
            var timelineEnd = new DateTime(2020, 03, 31);

            var response = await Client.TestClientGetAsync<TestResponse>(
                $"/departments/{user.Department}/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}"
            );

            var result = response.Value.value.FirstOrDefault(x => x.azureUniquePersonId == user.AzureUniqueId);
            result.pendingRequests.Should().Contain(x => x.Id == request.Id);
            result.timeline.Single().items.Should().Contain(x => x.type == "Request" && x.id == request.Id.ToString());
        }

        [Fact]
        public async Task TimeLineItem_ShouldIncludeChangeRequestState()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition()
                .WithInstances(x => x.AddInstance(new DateTime(2000, 01, 01), TimeSpan.FromDays(365)))
                .WithAssignedPerson(user);

            var instance = position.Instances.First();
            var bp = position.BasePosition;

            user.Positions.Add(new ApiPersonPositionV3
            {
                Id = position.Id,
                PositionId = position.Id,
                AppliesFrom = instance.AppliesFrom,
                AppliesTo = instance.AppliesTo,
                Workload = instance.Workload,
                Obs = instance.Obs,
                Name = position.Name,
                BasePosition = new ApiPersonBasePositionV3
                {
                    Id = bp.Id,
                    Name = bp.Name,
                    Discipline = bp.Discipline,
                    SubDiscipline = bp.SubDiscipline,
                    Type = bp.ProjectType
                },
                Project = new ApiPersonPositionProjectV3
                {
                    Id = testProject.Project.ProjectId,
                    DomainId = testProject.Project.DomainId,
                    Name = testProject.Project.Name
                }
            });

            var request = await Client.CreateDefaultResourceOwnerRequestAsync(user.FullDepartment, testProject,
                setup: x =>
                {
                    x.AsTypeResourceOwner("adjustment");
                    x.OrgPositionId = position.Id;
                    x.OrgPositionInstanceId = position.Instances.First().Id;
                },
                positionSetup: x =>
                {
                    x.Id = position.Id;
                    x.Instances = position.Instances;
                });
            await Client.ProposeChangesAsync(request.Id, new { workload = 50 });
            await Client.SetChangeParamsAsync(request.Id, DateTime.Today.AddDays(1));

            var timelineStart = request.OrgPositionInstance.AppliesFrom.Date.AddDays(-7).ToUniversalTime();
            var timelineEnd = request.OrgPositionInstance.AppliesTo.Date.AddDays(7).ToUniversalTime();

            var endpoint = $"/departments/{user.Department}/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}";
            var response = await Client.TestClientGetAsync<TestResponse>(endpoint);

            var result = response.Value.value.FirstOrDefault(x => x.azureUniquePersonId == user.AzureUniqueId);
            var timelinePostion = result.positionInstances.FirstOrDefault(x => x.PositionId == position.Id);
            timelinePostion.Should().NotBeNull();
            timelinePostion.HasChangeRequest.Should().BeTrue();
            timelinePostion.ChangeRequestStatus.Should().NotBeNull();
        }

        [Fact]
        public async Task GetRequestsOnPerson_ShouldNotIncludeRequestsOutsideCurrentAllocations()
        {
            TestApiInternalRequestModel request;
            using var adminScope = fixture.AdminScope();
            request = await Client.CreateDefaultRequestAsync(testProject, positionSetup: pos => pos.WithInstances(x =>
            {
                x.AddInstance(new DateTime(2021, 10, 01), TimeSpan.FromDays(30));
            }));
            await Client.AssignDepartmentAsync(request.Id, user.FullDepartment);
            await Client.ProposePersonAsync(request.Id, user);

            var endpoint = $"/departments/{user.Department}/resources/personnel/{user.AzureUniqueId}?includeCurrentAllocations=true&{ApiVersion}";
            var response = await Client.TestClientGetAsync<TestApiPersonnelPerson>(endpoint);

            response.Should().BeSuccessfull();
            var positionInstances = response.Value.positionInstances;
            positionInstances.Any(x => x.AppliesFrom > DateTime.Now || x.AppliesTo < DateTime.Now).Should().BeFalse();
        }

        [Fact]
        public async Task GetRequestsOnPerson_ShouldIncludeRequestsWithAppliesToDateNullWithCurrentAllocations()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject, positionSetup: pos => pos.WithInstances(x =>
            {
                x.AddInstance(new DateTime(2021, 10, 01), TimeSpan.FromDays(30));
            }));

            await Client.AddAbsence(user, x =>
            {
                x.AppliesFrom = new DateTime(2021, 08, 06);
                x.AppliesTo = null;
            });

            await Client.AssignDepartmentAsync(request.Id, user.FullDepartment);
            await Client.ProposePersonAsync(request.Id, user);

            var endpoint = $"/departments/{user.Department}/resources/personnel/{user.AzureUniqueId}?includeCurrentAllocations=true&{ApiVersion}";
            var response = await Client.TestClientGetAsync<TestApiPersonnelPerson>(endpoint);

            response.Should().BeSuccessfull();

            var absence = response.Value.employmentStatuses;

            absence.Any(x => x.appliesFrom > DateTime.Now || x.appliesTo < DateTime.Now).Should().BeFalse();
        }

        [Fact]
        public async Task GetTimeline_ShouldNotAggregateWorkloadWithPendingRequests()
        {
            TestApiInternalRequestModel request;
            using var adminScope = fixture.AdminScope();
            request = await Client.CreateDefaultRequestAsync(testProject, positionSetup: pos => pos.WithInstances(x =>
            {
                x.AddInstance(new DateTime(2020, 03, 02), TimeSpan.FromDays(15));
            }));
            await Client.AssignDepartmentAsync(request.Id, user.FullDepartment);
            await Client.ProposePersonAsync(request.Id, user);

            var timelineStart = new DateTime(2020, 03, 01);
            var timelineEnd = new DateTime(2020, 03, 31);

            var endpoint = $"/departments/{user.Department}/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}";
            var response = await Client.TestClientGetAsync<TestResponse>(endpoint);

            var result = response.Value.value.FirstOrDefault(x => x.azureUniquePersonId == user.AzureUniqueId);
            result.pendingRequests.Should().Contain(x => x.Id == request.Id);
            result.timeline.Single().items.Should().Contain(x => x.type == "Request" && x.id == request.Id.ToString());
            result.timeline.Single().workload.Should().Be(0);
        }

        [Fact]
        public async Task GetTimeline_ShouldNot_Include_Requests_That_Are_Completed_In_Pending_Requests()
        {
            TestApiInternalRequestModel request;
            using var adminScope = fixture.AdminScope();
            request = await Client.CreateDefaultRequestAsync(testProject, positionSetup: pos => pos.WithInstances(x =>
            {
                x.AddInstance(new DateTime(2020, 03, 02), TimeSpan.FromDays(15));
            }));
            await Client.AssignDepartmentAsync(request.Id, user.FullDepartment);
            await Client.ProposePersonAsync(request.Id, user);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            await Client.ResourceOwnerApproveAsync(user.Department, request.Id);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            var timelineStart = new DateTime(2020, 03, 01);
            var timelineEnd = new DateTime(2020, 03, 31);

            var endpoint = $"/departments/{user.Department}/resources/personnel/?$expand=timeline&{ApiVersion}&timelineStart={timelineStart:O}&timelineEnd={timelineEnd:O}";
            var response = await Client.TestClientGetAsync<TestResponse>(endpoint);

            var result = response.Value.value.FirstOrDefault(x => x.azureUniquePersonId == user.AzureUniqueId);
            result.Should().NotBeNull();
            result!.pendingRequests.Should().BeNull();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateChangeRequest_ShouldFail_WhenRemovingLocation(bool useLocationObject)
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject, positionSetup: pos => pos.WithInstances(x =>
            {
                x.AddInstance(new DateTime(2020, 03, 02), TimeSpan.FromDays(15));
            }));
            await Client.AssignDepartmentAsync(request.Id, user.FullDepartment);
            await Client.ProposePersonAsync(request.Id, user);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            await Client.ResourceOwnerApproveAsync(user.Department, request.Id);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            var payload = JObject.FromObject(new
            {
                type = "ResourceOwnerChange",
                subtype = "adjustment",
                orgPositionId = request.OrgPositionId,
                orgPositionInstanceId = request.OrgPositionInstanceId,
                proposedChanges = new Dictionary<string, object>
                {
                    ["location"] = useLocationObject
                        ? new
                        {
                            name = (string)null,
                        }
                        : null,
                }
            });

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>(
                    $"departments/{user.FullDepartment}/resources/requests", payload);
            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task CreateChangeRequest_ShouldSucceed_WhenProposingLocation()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject, positionSetup: pos => pos.WithInstances(x =>
            {
                x.AddInstance(new DateTime(2020, 03, 02), TimeSpan.FromDays(15));
            }));
            await Client.AssignDepartmentAsync(request.Id, user.FullDepartment);
            await Client.ProposePersonAsync(request.Id, user);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            await Client.ResourceOwnerApproveAsync(user.Department, request.Id);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            var payload = JObject.FromObject(new
            {
                type = "ResourceOwnerChange",
                subtype = "adjustment",
                orgPositionId = request.OrgPositionId,
                orgPositionInstanceId = request.OrgPositionInstanceId,
                proposedChanges = new Dictionary<string, object>
                {
                    ["location"] = new
                    {
                        name = "Top secret location",
                    }
                }
            });

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>(
                    $"departments/{user.FullDepartment}/resources/requests", payload);
            response.Should().BeSuccessfull();
        }


        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }

        class TestResponse
        {
            public List<TestApiPersonnelPerson> value { get; set; }
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
