using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
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
    public class ProjectRequestAccessTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
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

        public ProjectRequestAccessTests(ResourceApiFixture fixture, ITestOutputHelper output)
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

        [Theory]
        [InlineData("projectMember", true)]
        [InlineData("normalEmployee", false)]
        public async Task GetProjectRequests_When(string testCase, bool shouldHaveAccess)
        {            
            var projectMemberUser = fixture.AddProfile(FusionAccountType.Employee);
            
            if (testCase == "projectMember")
                projectMemberUser.WithPosition(testProject.AddPosition().WithEnsuredFutureInstances().WithAssignedPerson(projectMemberUser));

            using var projectMemberScope = fixture.UserScope(projectMemberUser);

            var resp = await Client.TestClientGetAsync<object>($"/projects/{projectId}/resources/requests");
            
            if (shouldHaveAccess)
                resp.Should().BeSuccessfull();
            else
                resp.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("projectMember", true)]
        [InlineData("normalEmployee", false)]
        public async Task GetRequestInProject_When(string testCase, bool shouldHaveAccess)
        {
            var projectMemberUser = fixture.AddProfile(FusionAccountType.Employee);

            if (testCase == "projectMember")
                projectMemberUser.WithPosition(testProject.AddPosition().WithEnsuredFutureInstances().WithAssignedPerson(projectMemberUser));

            using var projectMemberScope = fixture.UserScope(projectMemberUser);

            var resp = await Client.TestClientGetAsync<object>($"/projects/{projectId}/resources/requests/{normalRequest.Id}");

            if (shouldHaveAccess)
                resp.Should().BeSuccessfull();
            else
                resp.Should().BeUnauthorized();
        }

        [Fact]
        public async Task StartAllocationRequest_ShouldHaveAccess_WhenEditAccessOnPosition()
        {
            // Setup org api mock to return PUT in option call
            using var i = OrgRequestMocker.InterceptOption($"/{normalRequest.OrgPositionId}").RespondWithHeaders(HttpStatusCode.NoContent, h => h.Add("Allow", "PUT"));
                
            var projectMemberUser = fixture.AddProfile(FusionAccountType.Employee);


            using var projectMemberScope = fixture.UserScope(projectMemberUser);

            var resp = await Client.TestClientPostAsync<object>($"/projects/{projectId}/resources/requests/{normalRequest.Id}/start", null);
            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task StartAllocationRequest_ShouldNotAccess_WhenProjectMember()
        {
            var projectMemberUser = fixture.AddProfile(FusionAccountType.Employee);
            projectMemberUser.WithPosition(testProject.AddPosition().WithEnsuredFutureInstances().WithAssignedPerson(projectMemberUser));


            using var projectMemberScope = fixture.UserScope(projectMemberUser);

            var resp = await Client.TestClientPostAsync<object>($"/projects/{projectId}/resources/requests/{normalRequest.Id}/start", null);
            resp.Should().BeUnauthorized();
        }

        
        public Task DisposeAsync() => Task.CompletedTask;
    }

    public static class ApiPersonProfileV3Extensions
    {
        public static ApiPersonProfileV3 WithPosition(this ApiPersonProfileV3 profile, ApiPositionV2 position)
        {
            var personPositions = position.Instances
                .Where(i => i.AssignedPerson?.AzureUniqueId == profile.AzureUniqueId)
                .Select(i => new ApiPersonPositionV3()
                {
                    AppliesFrom = i.AppliesFrom,
                    AppliesTo = i.AppliesTo,
                    Id = i.Id,
                    BasePosition = new ApiPersonBasePositionV3()
                    {
                        Id = position.BasePosition.Id,
                        Discipline = position.BasePosition.Discipline,
                        Name = position.BasePosition.Name,
                        SubDiscipline = position.BasePosition.SubDiscipline,
                        Type = position.BasePosition.ProjectType
                    },
                    Name = position.Name,
                    Obs = i.Obs,
                    ParentPositionId = i.ParentPositionId,
                    PositionExternalId = position.ExternalId,
                    PositionId = position.Id,
                    Project = new ApiPersonPositionProjectV3()
                    {
                        Id = position.ProjectId,
                        DomainId = position.Project.DomainId,
                        Name = position.Project.Name,
                        Type = position.Project.ProjectType
                    },
                    Workload = i.Workload,
                    TaskOwnerIds = i.TaskOwnerIds
                }).ToList();

            if (profile.Positions is null)
                profile.Positions = new System.Collections.Generic.List<ApiPersonPositionV3>();

            profile.Positions.AddRange(personPositions);

            return profile;
        }
    }
}