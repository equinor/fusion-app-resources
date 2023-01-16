using FluentAssertions;
using Fusion.ApiClients.Org;
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
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class RequestActionTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private ApiPersonProfileV3 resourceOwner;
        private FusionTestProjectBuilder testProject;
        private TestApiInternalRequestModel normalRequest;
        private ApiPersonProfileV3 taskOwner;
        private ApiClients.Org.ApiPositionV2 taskOwnerPosition;
        private ApiClients.Org.ApiPositionV2 testPosition;
        private OrgRequestInterceptor orgInterceptor;

        public RequestActionTests(ResourceApiFixture fixture, ITestOutputHelper output)
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
        public async Task CreateRequestAction_ShouldBeSuccesfull()
        {
            var assignedPerson = fixture.AddProfile(FusionAccountType.Employee);
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
                responsible = "TaskOwner",
                isRequired = true,
                assignedToId = assignedPerson.AzureUniqueId,
                dueDate = "2021-10-02"
            };

            var result = await adminClient.TestClientPostAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions", payload);

            result.Should().BeSuccessfull();
            result.Value.id.Should().NotBeEmpty();
            result.Value.title.Should().Be(payload.title);
            result.Value.type.Should().Be(payload.type);
            result.Value.subType.Should().Be(payload.subType);
            result.Value.source.Should().Be(payload.source);
            result.Value.responsible.Should().Be(payload.responsible);
            result.Value.isResolved.Should().BeFalse();
            result.Value.resolvedAt.Should().BeNull();
            result.Value.resolvedBy.Should().BeNull();
            result.Value.isRequired.Should().BeTrue();
            result.Value.assignedTo.AzureUniquePersonId.Should().Be(assignedPerson.AzureUniqueId.Value);
            result.Value.dueDate.Should().Be(new DateTime(2021, 10, 02));
        }

        [Fact]
        public async Task CreateRequestAction_Body_ShouldBeOptional()
        {
            var assignedPerson = fixture.AddProfile(FusionAccountType.Employee);
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            var payload = new
            {
                title = "Test title",
                category = "Test category",
                type = "test",
            };

            var result = await adminClient.TestClientPostAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions", payload);

            result.Should().BeSuccessfull();
            result.Value.id.Should().NotBeEmpty();
            result.Value.title.Should().Be(payload.title);
            result.Value.type.Should().Be(payload.type);
            result.Value.isResolved.Should().BeFalse();
            result.Value.resolvedAt.Should().BeNull();
            result.Value.resolvedBy.Should().BeNull();
            result.Value.isRequired.Should().BeFalse();
        }

        [Fact]
        public async Task CreateRequestAction_ShouldGiveBadRequest_WhenAssignedToIdIsInvalid()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            var payload = new
            {
                title = "Test title",
                category = "Test category",
                type = "test",
                assignedToId = Guid.NewGuid()
            };

            var result = await adminClient.TestClientPostAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions", payload);

            result.Should().BeBadRequest();
        }

        [Fact]
        public async Task CreateRequestAction_ShouldGiveBadRequest_WhenRequestIsProvisioned()
        {
            const string testDepartment = "TPD PRD FE MMS MAT1";
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            var person = fixture.AddProfile(FusionAccountType.Employee);

            fixture.EnsureDepartment(testDepartment);
            var request = await adminClient.CreateDefaultRequestAsync(testProject, x => x.WithAssignedDepartment(testDepartment));
            await adminClient.StartProjectRequestAsync(testProject, request.Id);
            await adminClient.ProposePersonAsync(request.Id, person);
            await adminClient.ResourceOwnerApproveAsync(testDepartment, request.Id);
            await adminClient.TaskOwnerApproveAsync(testProject, request.Id);
            await adminClient.ProvisionRequestAsync(request.Id);

            var payload = new
            {
                title = "Test title",
                category = "Test category",
                type = "test"
            };

            var result = await adminClient.TestClientPostAsync<TestApiRequestAction>($"/requests/{request.Id}/actions", payload);

            result.Should().BeBadRequest();
        }

        [Fact]
        public async Task PatchRequestAction_ShouldBeOk()
        {
            var assignedPerson = fixture.AddProfile(FusionAccountType.Employee);
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();
            var action = await adminClient.AddRequestActionAsync(normalRequest.Id);

            var payload = new
            {
                title = "Updated Test title",
                body = "Updated Test body",
                category = "Updated Test category",
                type = "Updated test",
                subType = (string)null,
                isRequired = true,
                assignedToId = assignedPerson.AzureUniqueId,
                dueDate = "2021-10-02"
            };
            var result = await adminClient.TestClientPatchAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions/{action.id}", payload);

            result.Should().BeSuccessfull();
            result.Value.title.Should().Be(payload.title);
            result.Value.type.Should().Be(payload.type);
            result.Value.subType.Should().Be(payload.subType);
            result.Value.isRequired.Should().BeTrue();
            result.Value.assignedTo.AzureUniquePersonId.Should().Be(assignedPerson.AzureUniqueId.Value);
            result.Value.dueDate.Should().Be(new DateTime(2021, 10, 02));
        }

        [Fact]
        public async Task Patch_ShouldGiveBadRequest_WhenAssignedToIdIsInvalid()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();
            var action = await adminClient.AddRequestActionAsync(normalRequest.Id);

            var payload = new
            {
                title = "Updated Test title",
                body = "Updated Test body",
                category = "Updated Test category",
                type = "Updated test",
                subType = (string)null,
                isRequired = true,
                assignedToId = Guid.NewGuid(),
                dueDate = "2021-10-02"
            };
            var result = await adminClient.TestClientPatchAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions/{action.id}", payload);

            result.Should().BeBadRequest();
        }

        [Fact]
        public async Task Patch_ShouldBeAbleToUnassignAction()
        {
            var assignedPerson = fixture.AddProfile(FusionAccountType.Employee);

            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();
            var action = await adminClient.AddRequestActionAsync(normalRequest.Id, x => { x.assignedToId = assignedPerson.AzureUniqueId; });

            var payload = new
            {
                title = "Updated Test title",
                body = "Updated Test body",
                category = "Updated Test category",
                type = "Updated test",
                subType = (string)null,
                isRequired = true,
                assignedToId = default(Guid?),
                dueDate = "2021-10-02"
            };
            var result = await adminClient.TestClientPatchAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions/{action.id}", payload);

            result.Should().BeSuccessfull();
            result.Value.assignedTo.Should().BeNull();
        }

        [Fact]
        public async Task Patch_ShouldNotUnassignActionByDefault()
        {
            var assignedPerson = fixture.AddProfile(FusionAccountType.Employee);

            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();
            var action = await adminClient.AddRequestActionAsync(normalRequest.Id, x => { x.assignedToId = assignedPerson.AzureUniqueId; });

            var payload = new
            {
                title = "Updated Test title",
                body = "Updated Test body",
                category = "Updated Test category",
                type = "Updated test",
                subType = (string)null,
                isRequired = true,
                dueDate = "2021-10-02"
            };
            var result = await adminClient.TestClientPatchAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions/{action.id}", payload);

            result.Should().BeSuccessfull();
            result.Value.assignedTo.Should().NotBeNull();
        }

        [Fact]
        public async Task ResolveRequestAction_ShouldSetResolvedMetadata()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();
            var action = await adminClient.AddRequestActionAsync(normalRequest.Id, responsible: "ResourceOwner");

            var userClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(resourceOwner)
                .AddTestAuthToken();

            var payload = new { isResolved = true };
            var result = await userClient.TestClientPatchAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions/{action.id}", payload);

            result.Should().BeSuccessfull();
            result.Value.isResolved.Should().BeTrue();
            result.Value.resolvedAt.Should().BeCloseTo(DateTimeOffset.Now, precision: new TimeSpan(2000));
            result.Value.resolvedBy.AzureUniquePersonId.Should().Be(resourceOwner.AzureUniqueId.Value);
        }

        [Fact]
        public async Task UnresolveRequestAction_ShouldClearResolvedMetadata()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();
            var action = await adminClient.AddRequestActionAsync(normalRequest.Id, responsible: "ResourceOwner");

            var userClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(resourceOwner)
                .AddTestAuthToken();

            var payload = new { isResolved = true };
            _ = await userClient.TestClientPatchAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions/{action.id}", payload);


            payload = new { isResolved = false };
            var result = await userClient.TestClientPatchAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions/{action.id}", payload);

            result.Should().BeSuccessfull();
            result.Value.isResolved.Should().BeFalse();
            result.Value.resolvedAt.Should().BeNull();
            result.Value.resolvedBy.Should().BeNull();
        }

        [Fact]
        public async Task PatchingCustomProps_ShouldBeOk()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var action = await adminClient.AddRequestActionAsync(normalRequest.Id, responsible: "ResourceOwner", new Dictionary<string, object>
            {
                ["customProp1"] = 123,
                ["customProp2"] = new DateTime(2021, 05, 05)
            });

            var userClient = fixture.ApiFactory.CreateClient()
               .WithTestUser(resourceOwner)
               .AddTestAuthToken();

            var payload = new
            {
                properties = new Dictionary<string, object>
                {
                    ["customProp2"] = new DateTime(2021, 03, 03)
                }
            };
            var result = await userClient.TestClientPatchAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions/{action.id}", payload);

            result.Should().BeSuccessfull();
            result.Value.properties["customProp1"].Should().Be(123);
            result.Value.properties["customProp2"].Should().Be(new DateTime(2021, 03, 03));
        }

        [Fact]
        public async Task UpdatingActionOnDifferentRequest_ShouldBeNotFound()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var action = await adminClient.AddRequestActionAsync(normalRequest.Id);
            var otherRequest = await adminClient.CreateDefaultRequestAsync(testProject);

            var userClient = fixture.ApiFactory.CreateClient()
               .WithTestUser(resourceOwner)
               .AddTestAuthToken();

            var payload = new { title = "Updated Test title" };
            var result = await userClient.TestClientPatchAsync<TestApiRequestAction>($"/requests/{otherRequest.Id}/actions/{action.id}", payload);
            result.Should().BeNotFound();
        }

        [Fact]
        public async Task DeleteAction_ShouldBeSuccessfull()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var action = await adminClient.AddRequestActionAsync(normalRequest.Id, responsible: "ResourceOwner");
            var userClient = fixture.ApiFactory.CreateClient()
               .WithTestUser(resourceOwner)
               .AddTestAuthToken();

            var result = await userClient.TestClientDeleteAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions/{action.id}");
            result.Should().BeSuccessfull();
        }

        [Fact]
        public async Task DeletingActionOnDifferentRequest_ShouldBeNotFound()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var action = await adminClient.AddRequestActionAsync(normalRequest.Id);
            var otherRequest = await adminClient.CreateDefaultRequestAsync(testProject);

            var userClient = fixture.ApiFactory.CreateClient()
               .WithTestUser(resourceOwner)
               .AddTestAuthToken();

            var result = await userClient.TestClientDeleteAsync<TestApiRequestAction>($"/requests/{otherRequest.Id}/actions/{action.id}");
            result.Should().BeNotFound();
        }

        [Fact]
        public async Task GetRequests_ShouldFilterBySource()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var resourceOwnerAction = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.source = "ResourceOwner");
            var taskOwnerAction = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.source = "TaskOwner");

            var userClient = fixture.ApiFactory.CreateClient()
               .WithTestUser(resourceOwner)
               .AddTestAuthToken();

            var result = await userClient.TestClientGetAsync<List<TestApiRequestAction>>($"/requests/{normalRequest.Id}/actions?$filter=Source eq 'ResourceOwner'");
            result.Should().BeSuccessfull();
            result.Value.All(x => x.source == "ResourceOwner");
        }

        [Theory]
        [InlineData("ResourceOwner")]
        [InlineData("TaskOwner")]
        public async Task UserShouldOnlySeeActionsTheyAreResponsibleFor(string userRole)
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var resourceOwnerAction = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.responsible = "ResourceOwner");
            var taskOwnerAction = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.responsible = "TaskOwner");
            var action = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.responsible = "Both");

            await ExecuteAsRole(userRole, async http =>
            {
                var result = await http.TestClientGetAsync<List<TestApiRequestAction>>($"/requests/{normalRequest.Id}/actions");
                result.Should().BeSuccessfull();
                result.Value.Should().NotBeEmpty();
                result.Value.Should().OnlyContain(x => x.responsible == userRole || x.responsible == "Both");
            });
        }


        [Theory]
        [InlineData("ResourceOwner", "TaskOwner", false)]
        [InlineData("ResourceOwner", "ResourceOwner", true)]
        [InlineData("TaskOwner", "TaskOwner", true)]
        [InlineData("TaskOwner", "ResourceOwner", false)]
        [InlineData("ResourceOwner", "Both", true)]
        [InlineData("TaskOwner", "Both", true)]

        public async Task UserShouldOnlyBeAbleToUpdateActionsTheyAreResponsibleFor(string userRole, string responsible, bool shouldAllow)
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var action = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.responsible = responsible);

            await ExecuteAsRole(userRole, async http =>
            {
                var payload = new
                {
                    title = "Updated Test title",
                    body = "Updated Test body",
                    category = "Updated Test category",
                    type = "Updated test",
                    subType = (string)null,
                    isRequired = true,
                    dueDate = "2021-10-02"
                };
                var result = await http.TestClientPatchAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions/{action.id}", payload);
                if (shouldAllow)
                    result.Should().BeSuccessfull();
                else
                    result.Should().BeUnauthorized();
            });
        }

        [Theory]
        [InlineData("ResourceOwner", "TaskOwner", false)]
        [InlineData("ResourceOwner", "ResourceOwner", true)]
        [InlineData("TaskOwner", "TaskOwner", true)]
        [InlineData("TaskOwner", "ResourceOwner", false)]
        [InlineData("ResourceOwner", "Both", true)]
        [InlineData("TaskOwner", "Both", true)]

        public async Task UserShouldOnlyBeAbleToDeleteActionsTheyAreResponsibleFor(string userRole, string responsible, bool shouldAllow)
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var action = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.responsible = responsible);

            await ExecuteAsRole(userRole, async http =>
            {
                var result = await http.TestClientDeleteAsync<TestApiRequestAction>($"/requests/{normalRequest.Id}/actions/{action.id}");
                if (shouldAllow)
                    result.Should().BeSuccessfull();
                else
                    result.Should().BeUnauthorized();
            });
        }

        [Fact]
        public async Task GetRequests_ShouldFilterByResponsible()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var resourceOwnerAction = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.responsible = "ResourceOwner");
            var taskOwnerAction = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.responsible = "TaskOwner");

            var userClient = fixture.ApiFactory.CreateClient()
               .WithTestUser(resourceOwner)
               .AddTestAuthToken();

            var result = await userClient.TestClientGetAsync<List<TestApiRequestAction>>($"/requests/{normalRequest.Id}/actions?$filter=Responsible eq 'ResourceOwner'");
            result.Should().BeSuccessfull();
            result.Value.All(x => x.responsible == "ResourceOwner");
        }

        [Fact]
        public async Task GetRequests_ShouldFilterByType()
        {
            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var resourceOwnerAction = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.type = "TestType");
            var taskOwnerAction = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.type = "AnotherType");

            var userClient = fixture.ApiFactory.CreateClient()
               .WithTestUser(resourceOwner)
               .AddTestAuthToken();

            var result = await userClient.TestClientGetAsync<List<TestApiRequestAction>>($"/requests/{normalRequest.Id}/actions?$filter=Type eq 'TestType'");
            result.Should().BeSuccessfull();
            result.Value.All(x => x.type == "TestType");
        }

        [Fact]
        public async Task GetActions_ShouldIncludeAssignedTo()
        {
            var assignedPerson = fixture.AddProfile(FusionAccountType.Employee);

            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var action = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.assignedToId = assignedPerson.AzureUniqueId);
            var result = await adminClient.TestClientGetAsync<List<TestApiRequestAction>>($"/requests/{normalRequest.Id}/actions");
            result.Should().BeSuccessfull();
            result.Value.All(x => x.assignedTo is not null).Should().BeTrue();
        }

        [Fact]
        public async Task GetActions_ShouldIncludeAssignedToWithFullDepartment()
        {
            var assignedPerson = fixture.AddProfile(FusionAccountType.Employee);

            var adminClient = fixture.ApiFactory.CreateClient()
                   .WithTestUser(fixture.AdminUser)
                   .AddTestAuthToken();

            var action = await adminClient.AddRequestActionAsync(normalRequest.Id, x => x.assignedToId = assignedPerson.AzureUniqueId);
            var result = await adminClient.TestClientGetAsync<List<TestApiRequestAction>>($"/requests/{normalRequest.Id}/actions");
            result.Should().BeSuccessfull();
            result.Value.All(x => x.assignedTo?.FullDepartment is not null).Should().BeTrue();
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

            if(role == "TaskOwner")
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
