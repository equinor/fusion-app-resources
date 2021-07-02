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
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class RequestTaskTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private ApiPersonProfileV3 testUser;
        private FusionTestProjectBuilder testProject;
        private TestApiInternalRequestModel normalRequest;

        public RequestTaskTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.External);
        }



        public async Task InitializeAsync()
        {
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
            normalRequest = await adminClient.CreateDefaultRequestAsync(testProject);
        }


        [Fact]
        public async Task CreateRequestTask_ShouldBeSuccesfull()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            var payload = new
            {
                title = "Test title",
                body = "Test body",
                category = "Test category",
                type = "test",
                subType = "Test Test",
                source = "ResourceOwner",
                responsible = "TaskOwner"
            };

            var result = await adminClient.TestClientPostAsync<TestApiRequestTask>($"/requests/{normalRequest.Id}/tasks", payload);

            result.Should().BeSuccessfull();
            result.Value.id.Should().NotBeEmpty();
            result.Value.title.Should().Be(payload.title);
            result.Value.category.Should().Be(payload.category);
            result.Value.type.Should().Be(payload.type);
            result.Value.subType.Should().Be(payload.subType);
            result.Value.source.Should().Be(payload.source);
            result.Value.responsible.Should().Be(payload.responsible);
            result.Value.isResolved.Should().Be(false);
            result.Value.resolvedAt.Should().BeNull();
            result.Value.resolvedBy.Should().BeNull();
        }

        [Fact]
        public async Task PatchRequestTask_SHouldBeOk()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            var task = await CreateRequestTask();

            var payload = new
            {
                title = "Updated Test title",
                body = "Updated Test body",
                category = "Updated Test category",
                type = "Updated test",
                subType = (string)null,
            };
            var result = await adminClient.TestClientPatchAsync<TestApiRequestTask>($"/requests/{normalRequest.Id}/tasks/{task.id}", payload);

            result.Should().BeSuccessfull();
            result.Value.title.Should().Be(payload.title);
            result.Value.category.Should().Be(payload.category);
            result.Value.type.Should().Be(payload.type);
            result.Value.subType.Should().Be(payload.subType);
        }

        [Fact]
        public async Task ResolveRequestTask_ShouldSetResolvedMetadata()
        {
            var userClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(testUser)
                .AddTestAuthToken();

            var task = await CreateRequestTask();

            var payload = new { isResolved = true };
            var result = await userClient.TestClientPatchAsync<TestApiRequestTask>($"/requests/{normalRequest.Id}/tasks/{task.id}", payload);

            result.Should().BeSuccessfull();
            result.Value.isResolved.Should().BeTrue();
            result.Value.resolvedAt.Should().BeCloseTo(DateTimeOffset.Now);
            result.Value.resolvedBy.AzureUniquePersonId.Should().Be(testUser.AzureUniqueId.Value);

        }

        public async Task<TestApiRequestTask> CreateRequestTask()
        {
            var payload = new
            {
                title = "Test title",
                body = "Test body",
                category = "Test category",
                type = "test",
                subType = "Test Test",
                source = "ResourceOwner",
                responsible = "TaskOwner"
            };

            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            var result = await adminClient.TestClientPostAsync<TestApiRequestTask>($"/requests/{normalRequest.Id}/tasks", payload);
            result.Should().BeSuccessfull();
            return result.Value;
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
