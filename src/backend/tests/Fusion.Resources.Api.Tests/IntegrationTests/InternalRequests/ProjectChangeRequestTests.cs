using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Integration.Models.Queue;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
#nullable enable 

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class ProjectChangeRequestTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;

        /// <summary>
        ///     Will be generated new for each test
        /// </summary>
        private ApiPersonProfileV3 testUser;


        // Created by the async lifetime
        private TestApiInternalRequestModel request = null!;
        private FusionTestProjectBuilder testProject = null!;

        private Guid projectId => testProject.Project.ProjectId;

        public ProjectChangeRequestTests(ResourceApiFixture fixture, ITestOutputHelper output)
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
            request = await adminClient.CreateDefaultRequestAsync(testProject, r => r.AsTypeTaskOwnerChange());
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }

        #region Request flow tests

        #region Start
        [Fact]
        public async Task TOCRequest_Start_ShouldBeBadRequest_WhenNoPersonProposed()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{request.Id}/start", null);
            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task TOCRequest_Start_ShouldBeSuccessfull_WhenPersonIsProposed()
        {
            using var adminScope = fixture.AdminScope();

            var proposedPerson = fixture.AddProfile(FusionAccountType.Employee);
            await Client.ProposePersonAsync(request.Id, proposedPerson);

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{request.Id}/start", null);
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task TOCRequest_Start_ShouldBeSuccessfull_WhenInstanceChangesIsProposed()
        {
            using var adminScope = fixture.AdminScope();

            var proposedPerson = fixture.AddProfile(FusionAccountType.Employee);
            await Client.ProposeChangesAsync(request.Id, new { workload = 50 });

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{request.Id}/start", null);
            response.Should().BeSuccessfull();
        }

        [Theory]
        [InlineData("isDraft", false)]
        [InlineData("state", "created")]
        public async Task TOCRequest_Start_ShouldSet(string property, object value)
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_StartRequestAsync();

            var resp = await Client.TestClientGetAsync<JObject>($"/projects/{projectId}/requests/{request.Id}");
            resp.Should().BeSuccessfull();


            var propertyValue = resp.Value.GetValue(property, StringComparison.OrdinalIgnoreCase);
            var typedValue = propertyValue?.ToObject(value.GetType());

            typedValue.Should().Be(value);
        }

        [Fact]
        public async Task TOCRequest_Start_ShouldAddWorkflowInfo_WhenStartingRequest()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_StartRequestAsync();
            
            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{request.Id}", new { workflow = new { } });
            resp.Should().BeSuccessfull();
            resp.Value.workflow.Should().NotBeNull();
        }
        #endregion




        #endregion

        private async Task FastForward_StartRequestAsync(ApiPersonProfileV3? proposedPerson = null)
        {
            if (proposedPerson is null)
                proposedPerson = fixture.AddProfile(FusionAccountType.Employee);

            await Client.ProposePersonAsync(request.Id, proposedPerson);
            await Client.StartProjectRequestAsync(testProject, request.Id);

        }
    }

}