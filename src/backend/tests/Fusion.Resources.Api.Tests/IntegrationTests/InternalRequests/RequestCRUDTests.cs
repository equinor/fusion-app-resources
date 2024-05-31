using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
#nullable enable 

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    [Collection("Integration")]
    public class RequestCRUDTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;

        /// <summary>
        ///     Will be generated new for each test
        /// </summary>
        private ApiPersonProfileV3 testUser;


        // Created by the async lifetime
        private TestApiInternalRequestModel normalRequest = null!;
        private FusionTestProjectBuilder testProject = null!;

        private Guid projectId => testProject.Project.ProjectId;

        public RequestCRUDTests(ResourceApiFixture fixture, ITestOutputHelper output)
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

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(testProject.Project);

            // Prepare admin client
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            // Create a default request we can work with
            normalRequest = await adminClient.CreateDefaultRequestAsync(testProject, r => r.AsTypeNormal());
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }


        [Fact]
        public async Task Create_ShouldGetNewNumber()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition();
            var response = await Client.TestClientPostAsync($"/projects/{projectId}/requests", new
            {
                type = "normal",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id
            }, new
            {
                number = 0
            });

            response.Should().BeSuccessfull();
            response.Value.number.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Update_AssignRequest_ShouldPersist()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition();
            var orgUnit = fixture.AddOrgUnit("CRE ATE TST ADP A");

            var request = await Client.CreateDefaultRequestAsync(testProject);

            var response = await Client.TestClientPatchAsync($"/projects/{projectId}/requests/{request.Id}", new
            {
                assignedDepartment = orgUnit.FullDepartment
            }, new
            {
                assignedDepartment = string.Empty
            });

            response.Should().BeSuccessfull();
            response.Value.assignedDepartment.Should().Be(orgUnit.FullDepartment);
        }

        [Fact]
        public async Task Update_RemoveAssigndDepartment_ShouldPersist()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition();
            var orgUnit = fixture.AddOrgUnit("CRE ATE TST ADP B");

            var request = await Client.CreateDefaultRequestAsync(testProject, s => s.WithAssignedDepartment(orgUnit.FullDepartment));

            var response = await Client.TestClientPatchAsync($"/projects/{projectId}/requests/{request.Id}", new
            {
                assignedDepartment = (string?)null
            }, new
            {
                assignedDepartment = (string?)null
            });

            response.Should().BeSuccessfull();
            response.Value.assignedDepartment.Should().Be(null);
        }

        [Fact]
        public async Task Ceate_AssignedDepartment_ShouldSetAssingedSapId_WhenFullDepartmentProvided()
        {
            var testDepartmentString = "CRE ATE TST ADP";
            using var adminScope = fixture.AdminScope();
            var position = testProject.AddPosition();
            var orgUnit = fixture.AddOrgUnit(testDepartmentString);

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests", new
            {
                type = "normal",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id,
                assignedDepartment = testDepartmentString
            });
            response.Should().BeSuccessfull();
            response.Value.AssignedDepartment.Should().Be(testDepartmentString);

            using (var db = fixture.DbScope())
            {
                var req = await db.DbContext.ResourceAllocationRequests.FirstOrDefaultAsync(r => r.Id == response.Value.Id);
                req?.AssignedDepartmentId.Should().Be(orgUnit.SapId);
            }
        }

        [Fact]
        public async Task Ceate_AssignedDepartment_ShouldReturnFullDepartment_WhenAssignedDepartmentIsSapId()
        {
            var testDepartmentString = "CRE ATE TST ADP 2";
            using var adminScope = fixture.AdminScope();
            var position = testProject.AddPosition();
            var orgUnit = fixture.AddOrgUnit(testDepartmentString);

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests", new
            {
                type = "normal",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id,
                assignedDepartment = orgUnit.SapId
            });
            response.Should().BeSuccessfull();
            response.Value.AssignedDepartment.Should().Be(testDepartmentString);
        }

    }

}