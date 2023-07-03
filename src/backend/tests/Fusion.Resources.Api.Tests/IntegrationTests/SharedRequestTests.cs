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
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    [Collection("Integration")]
    public class SharedRequestTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private ApiPersonProfileV3 testUser;
        private FusionTestProjectBuilder testProject;
        private TestApiInternalRequestModel normalRequest;
        private System.Net.Http.HttpClient client;

        public SharedRequestTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.External);
        }

        public async Task InitializeAsync()
        {
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
            client = fixture.ApiFactory.CreateClient();
        }


        [Fact]
        public async Task CreateSharedRequest_ShouldBe_Success()
        {
            using var adminScope = fixture.AdminScope();
            var endpoint = $"/resources/requests/internal/{normalRequest.Id}/share";

            var share = new
            {
                scope = "Basic.Read",
                reason = "Test request sharing",
                sharedWith = new[]
                {
                    new { mail = testUser.Mail }
                }
            };
            var result = await client.TestClientPostAsync(endpoint, share);
            result.Should().BeSuccessfull();
        }

        [Fact]
        public async Task SharingRequest_Should_GrantAccess()
        {
            using var beforeSharing = fixture.UserScope(testUser);
            var result = await client.TestClientGetAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{normalRequest.Id}");
            result.Should().BeUnauthorized();

            using var adminScope = fixture.AdminScope();
            await client.ShareRequest(normalRequest.Id, testUser);

            using var userScope = fixture.UserScope(testUser);
            result = await client.TestClientGetAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{normalRequest.Id}");
            result.Should().BeSuccessfull();
        }

        [Fact]
        public async Task RevokingSharingRequest_Should_RevokeAccess()
        {
            using var adminScope = fixture.AdminScope();
            await client.ShareRequest(normalRequest.Id, testUser);

            using var beforeRevoke = fixture.UserScope(testUser);
            var result = await client.TestClientGetAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{normalRequest.Id}");
            result.Should().BeSuccessfull();

            using var adminDeleteScope = fixture.AdminScope();
            await client.TestClientDeleteAsync($"/resources/requests/internal/{normalRequest.Id}/share/{testUser.AzureUniqueId}");

            using var userScope = fixture.UserScope(testUser);
            result = await client.TestClientGetAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{normalRequest.Id}");
            result.Should().BeUnauthorized();
        }


        [Fact]
        public async Task GetUsersSharedRequests_ShouldNotContainRevoked()
        {
            using var adminScope = fixture.AdminScope();
            await client.ShareRequest(normalRequest.Id, testUser);

            using var beforeRevoke = fixture.UserScope(testUser);
            var result = await client.TestClientGetAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{normalRequest.Id}");
            result.Should().BeSuccessfull();

            using var adminDeleteScope = fixture.AdminScope();
            await client.TestClientDeleteAsync($"/resources/requests/internal/{normalRequest.Id}/share/{testUser.AzureUniqueId}");

            using var userScope = fixture.UserScope(testUser);
            var sharedReqResult = await client.TestClientGetAsync<ApiPagedCollection<TestApiInternalRequestModel>>($"/resources/persons/me/requests/shared");
            sharedReqResult.Value.Value.Should().NotContain(x => x.Id == normalRequest.Id);
        }

        [Fact]
        public async Task GetSharedRequests_Should_ReturnAllSharedRequests()
        {
            using var adminScope = fixture.AdminScope();
            await client.ShareRequest(normalRequest.Id, testUser);

            using var userScope = fixture.UserScope(testUser);
            var result = await client.TestClientGetAsync<ApiPagedCollection<TestApiInternalRequestModel>>($"resources/persons/me/requests/shared");
            result.Should().BeSuccessfull();
            result.Value.Value.Should().HaveCount(1);
            result.Value.Value.Should().Contain(x => x.Id == normalRequest.Id);
        }

        [Fact]
        public async Task GetSharedRequests_Should_ReturnAllSharedRequests_WhenMailIsId()
        {
            using var adminScope = fixture.AdminScope();
            await client.ShareRequest(normalRequest.Id, testUser);

            using var userScope = fixture.UserScope(testUser);
            var result = await client.TestClientGetAsync<ApiPagedCollection<TestApiInternalRequestModel>>($"resources/persons/{testUser.Mail}/requests/shared");
            result.Should().BeSuccessfull();
            result.Value.Value.Should().HaveCount(1);
            result.Value.Value.Should().Contain(x => x.Id == normalRequest.Id);
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();
            return Task.CompletedTask;
        }
    }
}
