using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class SecondOpinionTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        record TestAddSecondOpinion
        {
            public string Title { get; set; } = "Test Second Opinion";
            public string Description { get; set; } = "Test Second Opinion Description";
            public List<TestApiPerson> AssignedTo { get; init; } = new();
        }

        record TestSecondOpinionPrompt
        {
            public Guid Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public TestApiPerson CreatedBy { get; set; }
            public List<TestSecondOpinionResponse> Responses { get; set; } = new();
        }

        record TestSecondOpinionResponse
        {
            public Guid Id { get; set; }
            public TestApiPerson AssignedTo { get; set; } = null!;
            public DateTimeOffset? AnsweredAt { get; set; }

            public string Comment { get; set; }
            public string State { get; set; }
        }

        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private FusionTestProjectBuilder testProject;
        private ApiPersonProfileV3 testUser;

        public SecondOpinionTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);
        }

        private HttpClient Client => fixture.ApiFactory.CreateClient();

        public Task InitializeAsync()
        {
            testProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .AddToMockService();
            fixture.ContextResolver.AddContext(testProject.Project);

            testUser = fixture.AddProfile(FusionAccountType.Employee);

            return Task.CompletedTask;
        }

        [Fact]
        public async Task CreateSecondOpinion_ShouldBeSuccessful()
        {
            using var adminScope = fixture.AdminScope();

            var request = await Client.CreateDefaultRequestAsync(testProject);

            var payload = new TestAddSecondOpinion() with
            {
                Description = "Please add your second opinion for this req.",
                AssignedTo = new() { new TestApiPerson { Mail = testUser.Mail } }
            };

            var result = await Client.TestClientPostAsync<TestSecondOpinionPrompt>($"/resources/requests/internal/{request.Id}/second-opinions", payload);
            result.Should().BeSuccessfull();

            result.Value.Description.Should().Be(payload.Description);
            result.Value.CreatedBy.Mail.Should().Be(fixture.AdminUser.Mail);
            result.Value.Responses.Should().HaveCount(1);
            result.Value.Responses.Should().Contain(x => x.AssignedTo.AzureUniquePersonId == testUser.AzureUniqueId);
        }

        [Fact]
        public async Task CreateSecondOpinion_ShouldBeBadRequest_WhenNotAssignedToAnyone()
        {
            using var adminScope = fixture.AdminScope();

            var request = await Client.CreateDefaultRequestAsync(testProject);

            var payload = new TestAddSecondOpinion();

            var result = await Client.TestClientPostAsync<TestAddSecondOpinion>($"/resources/requests/internal/{request.Id}/second-opinions", payload);
            result.Should().BeBadRequest();
        }

        [Fact]
        public async Task CreateSecondOpionion_ShouldNotCreateDuplicates()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var secondOpinion = await CreateSecondOpinion(request, testUser, testUser, testUser);
            secondOpinion.Responses.Should().HaveCount(1);
        }


        [Fact]
        public async Task CreateSecondOpinion_ShouldFail_WhenUserDoesNotExist()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var payload = new TestAddSecondOpinion() with
            {
                AssignedTo = new()
                {
                    new TestApiPerson { Mail = "gjhkasdasd@equinor.com" },
                }
            };

            var result = await Client.TestClientPostAsync<TestSecondOpinionPrompt>($"/resources/requests/internal/{request.Id}/second-opinions", payload);
            result.Should().BeBadRequest();
        }

        [Fact]
        public async Task CreateSecondOpinion_ShouldFail_WhenRequestIsCompleted()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            
            await Client.StartProjectRequestAsync(testProject, request.Id);
            await Client.ResourceOwnerApproveAsync("PDP PRD FE ANE", request.Id);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            var payload = new TestAddSecondOpinion() with
            {
                AssignedTo = new() { new TestApiPerson { Mail = testUser.Mail } }
            };

            var result = await Client.TestClientPostAsync<TestSecondOpinionPrompt>($"/resources/requests/internal/{request.Id}/second-opinions", payload);
            result.Should().BeBadRequest();
        }

        [Fact]
        public async Task CreateSecondOpinion_ShouldShareRequest()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            var secondOpinion = await CreateSecondOpinion(request, testUser);

            var userSharedRequests = await Client.TestClientGetAsync<ApiPagedCollection<TestApiInternalRequestModel>>($"resources/persons/{testUser.AzureUniqueId}/requests/shared");
            userSharedRequests.Value.Value.Should().Contain(x => x.Id == request.Id);
        }

        [Fact]
        public async Task AssignSecondOpinion_ShouldShareRequest()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var secondOpinion = await CreateSecondOpinion(request, testUser);


            var userToAdd = fixture.AddProfile(FusionAccountType.Employee);
            var payload = new TestAddSecondOpinion() with
            {
                AssignedTo = new()
                {
                    new TestApiPerson { Mail = testUser.Mail },
                    new TestApiPerson { Mail = userToAdd.Mail }
                }
            };
            var endpoint = $"/resources/requests/internal/{request.Id}/second-opinions/{secondOpinion.Id}";
            var result = await Client.TestClientPatchAsync<TestSecondOpinionPrompt>(endpoint, payload);
            result.Should().BeSuccessfull();

            using var userScope = fixture.UserScope(userToAdd);
            var userSharedRequests = await Client.TestClientGetAsync<ApiPagedCollection<TestApiInternalRequestModel>>($"resources/persons/me/requests/shared");
            userSharedRequests.Value.Value.Should().Contain(x => x.Id == request.Id);
        }

        [Fact]
        public async Task UnassignSecondOpinion_ShouldRemoveResponse()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var userToRemove = fixture.AddProfile(FusionAccountType.Employee);
            var secondOpinion = await CreateSecondOpinion(request, testUser, userToRemove);

            var payload = new TestAddSecondOpinion() with
            {
                AssignedTo = new() { new TestApiPerson { Mail = testUser.Mail } }
            };
            var endpoint = $"/resources/requests/internal/{request.Id}/second-opinions/{secondOpinion.Id}";
            var result = await Client.TestClientPatchAsync<TestSecondOpinionPrompt>(endpoint, payload);

            result.Should().BeSuccessfull();
            result.Value.Responses.Should().NotContain(x => x.AssignedTo.AzureUniquePersonId == userToRemove.AzureUniqueId);
        }

        [Fact]
        public async Task UnassignSecondOpinion_ShouldRevokeShare()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var userToRemove = fixture.AddProfile(FusionAccountType.Employee);
            var secondOpinion = await CreateSecondOpinion(request, testUser, userToRemove);

            var payload = new TestAddSecondOpinion() with
            {
                AssignedTo = new() { new TestApiPerson { Mail = testUser.Mail } }
            };
            var endpoint = $"/resources/requests/internal/{request.Id}/second-opinions/{secondOpinion.Id}";
            var result = await Client.TestClientPatchAsync<TestSecondOpinionPrompt>(endpoint, payload);
            result.Should().BeSuccessfull();

            using var userScope = fixture.UserScope(userToRemove);
            var userSharedRequests = await Client.TestClientGetAsync<ApiPagedCollection<TestApiInternalRequestModel>>($"resources/persons/me/requests/shared");
            userSharedRequests.Value.Value.Should().NotContain(x => x.Id == request.Id);
        }

        [Fact]
        public async Task PatchSecondOpinion_ShouldSetAllFields()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var secondOpinion = await CreateSecondOpinion(request, testUser);
            var payload = new
            {
                title = "Updated title",
                description = "Updated description",
            };

            var endpoint = $"/resources/requests/internal/{request.Id}/second-opinions/{secondOpinion.Id}";
            var patchResult = await Client.TestClientPatchAsync<TestSecondOpinionPrompt>(endpoint, payload);
            patchResult.Should().BeSuccessfull();

            var result = await Client.TestClientGetAsync<List<TestSecondOpinionPrompt>>($"/resources/requests/internal/{request.Id}/second-opinions/");
            result.Value.First().Title.Should().Be(payload.title);
            result.Value.First().Description.Should().Be(payload.description);
        }

        [Fact]
        public async Task PatchSecondOpionion_ShouldNotCreateDuplicates()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var secondOpinion = await CreateSecondOpinion(request, testUser);

            var payload = new TestAddSecondOpinion() with
            {
                AssignedTo = new()
                {
                    new TestApiPerson { Mail = testUser.Mail },
                    new TestApiPerson { Mail = testUser.Mail },
                    new TestApiPerson { Mail = testUser.Mail }
                }
            };
            var endpoint = $"/resources/requests/internal/{request.Id}/second-opinions/{secondOpinion.Id}";
            var result = await Client.TestClientPatchAsync<TestSecondOpinionPrompt>(endpoint, payload);
            result.Value.Responses.Should().HaveCount(1);
        }

        [Fact]
        public async Task PatchSecondOpinion_ShouldFail_WhenUserDoesNotExist()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var secondOpinion = await CreateSecondOpinion(request, testUser);


            var payload = new TestAddSecondOpinion() with
            {
                AssignedTo = new()
                {
                    new TestApiPerson { Mail = "gjhkasdasd@equinor.com" },
                }
            };

            var endpoint = $"/resources/requests/internal/{request.Id}/second-opinions/{secondOpinion.Id}";
            var result = await Client.TestClientPatchAsync<TestSecondOpinionPrompt>(endpoint, payload);
            result.Should().BeBadRequest();
            result.Content.Should().Contain(payload.AssignedTo.First().Mail);
        }

        [Fact]
        public async Task GetSecondOpinionResponses_ShouldOnlyReturnAssigneesResponses()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var anotherUser = fixture.AddProfile(FusionAccountType.Employee);
            var secondOpinion = await CreateSecondOpinion(request, testUser, anotherUser);


            using var userScope = fixture.UserScope(testUser);
            var userSharedOpinions = await Client.TestClientGetAsync<List<TestSecondOpinionResponse>>("/persons/me/second-opinions/responses");

            var allResponses = userSharedOpinions.Value;
            allResponses.Should().OnlyContain(x => x.AssignedTo.AzureUniquePersonId == testUser.AzureUniqueId);
        }

        [Fact]
        public async Task GetSecondOpinion_ShouldNotShowDraftsForOtherUsers()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var secondOpinion = await CreateSecondOpinion(request, testUser);

            var payload = new
            {
                Comment = "This is my comment",
                State = "Draft"
            };

            using var userScope = fixture.UserScope(testUser);
            var result = await Client.TestClientPatchAsync<TestSecondOpinionPrompt>($"/resources/requests/internal/{request.Id}/second-opinions/{secondOpinion.Id}/responses/{secondOpinion.Responses.First().Id}", payload);

            using var adminScope2 = fixture.AdminScope();
            var secondOpinions = await Client.TestClientGetAsync<List<TestSecondOpinionPrompt>>($"/resources/requests/internal/{request.Id}/second-opinions");
            secondOpinions.Value.First().Responses.First().Comment.Should().BeEmpty();
        }


        [Fact]
        public async Task ClosingRequest_ShouldHidePublishedSecondOpinions()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var secondOpinion = await CreateSecondOpinion(request, testUser);

            foreach (var response in secondOpinion.Responses)
            {
                await AddResponse(request.Id, secondOpinion.Id, response.Id);
            }

            await Client.ResourceOwnerApproveAsync("PDP PRD FE ANE", request.Id);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            var result = await Client.TestClientGetAsync<List<TestSecondOpinionPrompt>>($"/resources/requests/internal/{request.Id}/second-opinions/");
            var prompt = result.Value.First();

            prompt.Responses
                .All(x => x.Comment == "Comments are hidden when request is closed.")
                .Should().BeTrue();
        }

        [Fact]
        public async Task ClosingRequest_ShouldCloseUnpublishedSecondOpinions()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var secondOpinion = await CreateSecondOpinion(request, testUser);

            await Client.ResourceOwnerApproveAsync("PDP PRD FE ANE", request.Id);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            var result = await Client.TestClientGetAsync<List<TestSecondOpinionPrompt>>($"/resources/requests/internal/{request.Id}/second-opinions/");
            var prompt = result.Value.First();

            prompt.Responses.All(x => x.State == "Closed")
                .Should().BeTrue();
        }



        [Fact]
        public async Task GetPersonalSecondOpinions_ShouldSupportNotFiltering()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var endpoint = $"/persons/me/second-opinions";
            var secondOpinion = await CreateSecondOpinion(request, testUser);

            var result = await Client.TestClientGetAsync<List<TestSecondOpinionPrompt>>(endpoint);
            result.Value.Should().Contain(x => x.Id == secondOpinion.Id);
        }

        [Fact]
        public async Task GetPersonalSecondOpinions_ShouldFilterByRequestState()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var endpoint = $"/persons/me/second-opinions/?$filter=state eq 'Active'";
            var secondOpinion = await CreateSecondOpinion(request, testUser);

            var result = await Client.TestClientGetAsync<List<TestSecondOpinionPrompt>>(endpoint);
            result.Value.Should().Contain(x => x.Id == secondOpinion.Id);


            await Client.ResourceOwnerApproveAsync("PDP PRD FE ANE", request.Id);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            result = await Client.TestClientGetAsync<List<TestSecondOpinionPrompt>>(endpoint);
            result.Value.Should().NotContain(x => x.Id == secondOpinion.Id);
        }

        private async Task<TestSecondOpinionPrompt> CreateSecondOpinion(TestApiInternalRequestModel request, params ApiPersonProfileV3[] assignedTo)
        {
            var payload = new TestAddSecondOpinion() with
            {
                AssignedTo = assignedTo.Select(x => new TestApiPerson { Mail = x.Mail }).ToList()
            };

            var result = await Client.TestClientPostAsync<TestSecondOpinionPrompt>($"/resources/requests/internal/{request.Id}/second-opinions", payload);
            result.Should().BeSuccessfull();

            return result.Value;
        }

        private async Task AddResponse(Guid requestId, Guid secondOpinionId, Guid responseId, string state = "Published", string comment = "This is my comment")
        {
            var payload = new
            {
                Comment = comment,
                State = state
            };

            using var userScope = fixture.UserScope(testUser);
            var patchResult = await Client.TestClientPatchAsync<TestSecondOpinionPrompt>($"/resources/requests/internal/{requestId}/second-opinions/{secondOpinionId}/responses/{responseId}", payload);
            patchResult.Should().BeSuccessfull();
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
