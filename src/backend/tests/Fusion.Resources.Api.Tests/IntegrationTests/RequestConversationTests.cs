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
using System.Net;
using System.Net.Http;
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
        private ApiPersonProfileV3 resourceOwner;
        private FusionTestProjectBuilder testProject;
        private TestApiInternalRequestModel normalRequest;
        private ApiPersonProfileV3 taskOwner;
        private ApiClients.Org.ApiPositionV2 taskOwnerPosition;
        private OrgRequestInterceptor orgInterceptor;
        private ApiClients.Org.ApiPositionV2 testPosition;

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
            resourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            resourceOwner.IsResourceOwner = true;
            resourceOwner.FullDepartment = normalRequest.AssignedDepartment ?? "PDP TST DPT";

            taskOwner = fixture.AddProfile(FusionAccountType.Employee);
            taskOwner.IsResourceOwner = false;
            taskOwner = fixture.AddProfile(FusionAccountType.Employee);
            taskOwnerPosition = testProject.AddPosition()
                .WithAssignedPerson(taskOwner);

            fixture.ContextResolver
               .AddContext(testProject.Project);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = "PDP TST DPT");
            testPosition = testProject.AddPosition()
                .WithBasePosition(bp)
                .WithAssignedPerson(fixture.AddProfile(FusionAccountType.Employee))
                .WithEnsuredFutureInstances()
                .WithTaskOwner(taskOwnerPosition.Id);

            OrgServiceMock.SetTaskOwner(testPosition.Id, taskOwnerPosition.Id);
        }

        [Fact]
        public async Task AddMessage_Should_SetSenderMeta()
        {
            using var scope = fixture.UserScope(resourceOwner);
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

            result.Sender.AzureUniquePersonId.Should().Be(resourceOwner.AzureUniqueId.GetValueOrDefault());
            result.Sent.Should().BeCloseTo(DateTimeOffset.UtcNow, 5000);

            result.Properties["customProp1"].Should().Be(payload.properties.customProp1);
            result.Properties["customProp2"].Should().Be(payload.properties.customProp2);
        }

        [Fact]
        public async Task AddMessage_ShouldGiveNotFound_WhenRequestDoesNotExist()
        {
            using var scope = fixture.UserScope(resourceOwner);
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
            using var scope = fixture.UserScope(resourceOwner);
            var client = fixture.ApiFactory.CreateClient();
            var message = await client.AddRequestMessage(normalRequest.Id, recipient: "ResourceOwner", new Dictionary<string, object>
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

            result.Value.Sender.AzureUniquePersonId.Should().Be(resourceOwner.AzureUniqueId.GetValueOrDefault());
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

        [Theory]
        [InlineData("ResourceOwner")]
        [InlineData("TaskOwner")]
        public async Task GetConversation_ShouldOnlyIncludeTasksForRecipient(string role)
        {
            var adminClient = fixture.ApiFactory.CreateClient()
               .WithTestUser(fixture.AdminUser)
               .AddTestAuthToken();

            var first = await adminClient.AddRequestMessage(normalRequest.Id, recipient: "TaskOwner");
            var second = await adminClient.AddRequestMessage(normalRequest.Id, recipient: "ResourceOwner");

            await ExecuteAsRole(role, async http =>
            {
                var result = await http.TestClientGetAsync<List<TestApiRequestMessage>>($"/requests/internal/{normalRequest.Id}/conversation");
                result.Should().BeSuccessfull();
                result.Value.Should().NotBeEmpty();
                result.Value.Should().OnlyContain(x => x.Recipient == role);
            });
        }

        [Fact]
        public async Task GetConversation_ShouldGiveNotFound_WhenRequestDoesNotExist()
        {
            using var scope = fixture.AdminScope();
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientGetAsync<List<TestApiRequestMessage>>($"/requests/internal/{Guid.NewGuid()}/conversation");
            result.Should().BeNotFound();
        }


        [Theory]
        [InlineData("ResourceOwner", "TaskOwner", false)]
        [InlineData("ResourceOwner", "ResourceOwner", true)]
        [InlineData("TaskOwner", "TaskOwner", true)]
        [InlineData("TaskOwner", "ResourceOwner", false)]

        public async Task UserShouldOnlyBeAbleToUpdateConversationsWhenTheyAreRecepient(string userRole, string recipient, bool shouldAllow)
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var message = await adminClient.AddRequestMessage(normalRequest.Id, recipient: recipient);

            await ExecuteAsRole(userRole, async http =>
            {
                var payload = new
                {
                    title = "Hello, updated world!",
                    body = "Goodbye, updated world!",
                    category = "worldupdate",
                    recipient = recipient,
                };

                var result = await http.TestClientPutAsync<TestApiRequestMessage>($"/requests/internal/{normalRequest.Id}/conversation/{message.Id}", payload);

                if (shouldAllow)
                    result.Should().BeSuccessfull();
                else
                    result.Should().BeUnauthorized();
            });
        }

        private async Task ExecuteAsRole(string role, Func<HttpClient, Task> action)
        {
            var user = role switch
            {
                "ResourceOwner" => resourceOwner,
                "TaskOwner" => taskOwner,
                _ => throw new NotImplementedException()
            };

            var userClient = fixture.ApiFactory.CreateClient()
                     .WithTestUser(user)
                     .AddTestAuthToken();

            if (role == "TaskOwner")
            {
                using var i = orgInterceptor = OrgRequestMocker
                        .InterceptOption($"/{normalRequest.OrgPositionId}")
                        .RespondWithHeaders(HttpStatusCode.NoContent, h => h.Add("Allow", "PUT"));
                await action(userClient);
                return;
            }
            await action(userClient);
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
