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
    public class RequestConversationTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private ApiPersonProfileV3 testUser;
        private FusionTestProjectBuilder testProject;
        private TestApiInternalRequestModel normalRequest;

        public RequestConversationTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);
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

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.Employee);
            testUser.IsResourceOwner = true;
            testUser.FullDepartment = normalRequest.AssignedDepartment ?? "PDP TST DPT";
        }

        [Fact]
        public async Task AddMessage_Should_SetSenderMeta()
        {
            using var scope = fixture.UserScope(testUser);
            var client = fixture.ApiFactory.CreateClient();

            var payload = new
            {
                title = "Hello, world!",
                body = "Goodbye, world!",
                category = "world",
                recipient = "TaskOwner",
                properties = new
                {
                    customProp1 = 123,
                    customProp2 = new DateTime(2021, 07, 08)
                }
            };

            var result = await client.AddRequestMessage(normalRequest.Id, payload);

            result.Id.Should().NotBeEmpty();
            result.Title.Should().Be(payload.title);
            result.Body.Should().Be(payload.body);
            result.Recipient.Should().Be(payload.recipient);

            result.Sender.AzureUniquePersonId.Should().Be(testUser.AzureUniqueId.GetValueOrDefault());
            result.Sent.Should().BeCloseTo(DateTimeOffset.UtcNow, 5000);

            result.Properties["customProp1"].Should().Be(payload.properties.customProp1);
            result.Properties["customProp2"].Should().Be(payload.properties.customProp2);
        }

        [Fact]
        public async Task AddMessage_ShouldGiveNotFound_WhenRequestDoesNotExist()
        {
            using var scope = fixture.UserScope(testUser);
            var client = fixture.ApiFactory.CreateClient();

            var payload = new
            {
                title = "Hello, world!",
                body = "Goodbye, world!",
                category = "world",
                recipient = "TaskOwner",
                properties = new
                {
                    customProp1 = 123,
                    customProp2 = new DateTime(2021, 07, 08)
                }
            };

            var result = await client.TestClientPostAsync<TestApiRequestMessage>($"/requests/internal/{Guid.NewGuid()}/conversation", payload);
            result.Should().BeNotFound();
        }

        [Fact]
        public async Task UpdateMessage_Should_NotChangeSenderMeta()
        {
            using var scope = fixture.UserScope(testUser);
            var client = fixture.ApiFactory.CreateClient();
            var message = await client.AddRequestMessage(normalRequest.Id, new Dictionary<string, object>
            {
                ["customProp1"] = 123,
                ["customProp2"] = new DateTime(2021, 07, 07)
            });

            var payload = new
            {
                title = "Hello, updated world!",
                body = "Goodbye, updated world!",
                category = "worldupdate",
                recipient = "ResourceOwner",
                properties = new
                {
                    customProp1 = 654,
                    customProp2 = new DateTime(2021, 07, 09)
                }
            };

            var result = await client.TestClientPutAsync<TestApiRequestMessage>($"/requests/internal/{normalRequest.Id}/conversation/{message.Id}", payload);
            result.Should().BeSuccessfull();

            result.Value.Id.Should().NotBeEmpty();
            result.Value.Title.Should().Be(payload.title);
            result.Value.Body.Should().Be(payload.body);
            result.Value.Recipient.Should().Be(payload.recipient);

            result.Value.Sender.AzureUniquePersonId.Should().Be(testUser.AzureUniqueId.GetValueOrDefault());
            result.Value.Sent.Should().Be(message.Sent);

            result.Value.Properties["customProp1"].Should().Be(payload.properties.customProp1);
            result.Value.Properties["customProp2"].Should().Be(payload.properties.customProp2);
        }

        [Fact]
        public async Task UpdateMessage_Should_ReturnNotFound_WhenMessageDoesNotExist()
        {
            using var scope = fixture.AdminScope();
            var client = fixture.ApiFactory.CreateClient();

            var payload = new
            {
                title = "Hello, updated world!",
                body = "Goodbye, updated world!",
                category = "worldupdate",
                recipient = "ResourceOwner",
                properties = new
                {
                    customProp1 = 654,
                    customProp2 = new DateTime(2021, 07, 09)
                }
            };

            var result = await client.TestClientPutAsync<TestApiRequestMessage>($"/requests/internal/{normalRequest.Id}/conversation/{Guid.NewGuid()}", payload);
            result.Should().BeNotFound();
        }

        [Fact]
        public async Task UpdateMessage_Should_ReturnBadRequest_WhenNoTitle()
        {
            using var scope = fixture.AdminScope();
            var client = fixture.ApiFactory.CreateClient();

            var payload = new
            {
                body = "Goodbye, updated world!",
                category = "worldupdate",
                recipient = "ResourceOwner",
                properties = new
                {
                    customProp1 = 654,
                    customProp2 = new DateTime(2021, 07, 09)
                }
            };

            var result = await client.TestClientPutAsync<TestApiRequestMessage>($"/requests/internal/{normalRequest.Id}/conversation/{Guid.NewGuid()}", payload);
            result.Should().BeBadRequest();
        }

        [Fact]
        public async Task GetConversation_ShouldReturnAllMessages()
        {
            using var scope = fixture.AdminScope();
            var client = fixture.ApiFactory.CreateClient();

            var first = await client.AddRequestMessage(normalRequest.Id);
            var second = await client.AddRequestMessage(normalRequest.Id);

            var result = await client.TestClientGetAsync<List<TestApiRequestMessage>>($"/requests/internal/{normalRequest.Id}/conversation");
            result.Should().BeSuccessfull();

            result.Value.Should().Contain(x => x.Id == first.Id);
            result.Value.Should().Contain(x => x.Id == second.Id);
        }

        [Fact]
        public async Task GetConversation_ShouldGiveNotFound_WhenRequestDoesNotExist()
        {
            using var scope = fixture.AdminScope();
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientGetAsync<List<TestApiRequestMessage>>($"/requests/internal/{Guid.NewGuid()}/conversation");
            result.Should().BeNotFound();
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
