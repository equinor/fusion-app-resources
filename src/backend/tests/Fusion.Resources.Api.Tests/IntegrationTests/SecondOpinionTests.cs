﻿using FluentAssertions;
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
    [Collection("Integration")]
    public class SecondOpinionTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        const string TestDepartmentId = "PDP PRD FE ANE";
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

        record TestSecondOpinionResult
        {
            public List<TestSecondOpinionPrompt> Value { get; set; }
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

            fixture.EnsureDepartment(TestDepartmentId);

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
            await Client.AssignDepartmentAsync(request.Id, TestDepartmentId);
            await Client.ProposePersonAsync(request.Id, fixture.AddProfile(FusionAccountType.Employee));
            await Client.ResourceOwnerApproveAsync(TestDepartmentId, request.Id);
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

            var result = await Client.TestClientGetAsync<TestSecondOpinionResult>($"/resources/requests/internal/{request.Id}/second-opinions/");
            result.Value.Value.First().Title.Should().Be(payload.title);
            result.Value.Value.First().Description.Should().Be(payload.description);
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
        public async Task GetSecondOpinionResponses_ShouldOnlyReturnAssigneesUnpublishedResponses()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var anotherUser = fixture.AddProfile(FusionAccountType.Employee);
            var secondOpinion = await CreateSecondOpinion(request, testUser, anotherUser);


            using var userScope = fixture.UserScope(testUser);
            var userSharedOpinions = await Client.TestClientGetAsync<TestSecondOpinionResult>("/persons/me/second-opinions/responses");

            var allResponses = userSharedOpinions.Value.Value
                .SelectMany(x => x.Responses)
                .Where(x => x.State != "Published");
            allResponses.Should().OnlyContain(x => x.AssignedTo.AzureUniquePersonId == testUser.AzureUniqueId);
        }

        [Fact]
        public async Task GetSecondOpinionResponses_ShouldOnlyReturnPublishedResponsesFromOtherAssignees()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);

            var anotherUser = fixture.AddProfile(FusionAccountType.Employee);
            var publishingUser = fixture.AddProfile(FusionAccountType.Employee);

            var secondOpinion = await CreateSecondOpinion(request, testUser, anotherUser, publishingUser);

            foreach (var response in secondOpinion.Responses)
            {
                if (response.AssignedTo.AzureUniquePersonId == publishingUser.AzureUniqueId)
                    await AddResponse(request.Id, secondOpinion.Id, response.Id, publishingUser);
            }

            using var userScope = fixture.UserScope(testUser);
            var userSharedOpinions = await Client.TestClientGetAsync<TestSecondOpinionResult>("/persons/me/second-opinions/responses");

            var otherResponses = userSharedOpinions.Value.Value
                .SelectMany(x => x.Responses)
                .Where(x => x.AssignedTo.AzureUniquePersonId != testUser.AzureUniqueId);

            otherResponses.Should().NotBeEmpty();
            otherResponses.Should().OnlyContain(x => x.State == "Published");
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
            var secondOpinions = await Client.TestClientGetAsync<TestSecondOpinionResult>($"/resources/requests/internal/{request.Id}/second-opinions");
            secondOpinions.Value.Value.First().Responses.First().Comment.Should().BeEmpty();
        }


        [Fact]
        public async Task GetPersonalSecondOpinions_ShouldAllowCountOnly()
        {
            const string department = "TST ABC DEF GHI";
            fixture.EnsureDepartment(department);
            var resourceOwner = fixture.AddResourceOwner(department);

            using var adminScope = fixture.AdminScope();

            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);
            await Client.AssignDepartmentAsync(request.Id, department);


            using var userScope = fixture.UserScope(resourceOwner);

            var users = new List<ApiPersonProfileV3> { testUser };
            for (int i = 0; i < 5; i++)
            {
                users.Add(fixture.AddProfile(FusionAccountType.Employee));
            }

            var secondOpinion = await CreateSecondOpinion(request, users.ToArray());

            var response = secondOpinion.Responses.Single(x => x.AssignedTo.AzureUniquePersonId == testUser.AzureUniqueId);
            await AddResponse(request.Id, secondOpinion.Id, response.Id);

            var result = await Client.TestClientGetAsync($"/persons/me/second-opinions/?$count=only", new
            {
                counts = new { totalCount = 0, publishedCount = 0 },
                value = new[]
                {
                    new
                    {
                        id = Guid.NewGuid(),
                    }
                }
            });

            var counts = result.Value.counts;
            counts.totalCount.Should().Be(6);
            counts.publishedCount.Should().Be(1);
            result.Value.value.Should().BeEmpty();
        }

        [Fact]
        public async Task ClosingRequest_ShouldNotHidePublishedSecondOpinions()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var secondOpinion = await CreateSecondOpinion(request, testUser);

            foreach (var response in secondOpinion.Responses)
            {
                await AddResponse(request.Id, secondOpinion.Id, response.Id);
            }
            await Client.AssignDepartmentAsync(request.Id, TestDepartmentId);
            await Client.ProposePersonAsync(request.Id, fixture.AddProfile(FusionAccountType.Employee));
            await Client.ResourceOwnerApproveAsync(TestDepartmentId, request.Id);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            var result = await Client.TestClientGetAsync<TestSecondOpinionResult>($"/resources/requests/internal/{request.Id}/second-opinions/");
            var prompt = result.Value.Value.First();

            prompt.Responses
                .Should()
                .NotContain(x => x.Comment == "Comments are hidden when request is closed.");
        }

        [Fact]
        public async Task ClosingRequest_ShouldCloseUnpublishedSecondOpinions()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var secondOpinion = await CreateSecondOpinion(request, testUser);

            await Client.AssignDepartmentAsync(request.Id, TestDepartmentId);
            await Client.ProposePersonAsync(request.Id, fixture.AddProfile(FusionAccountType.Employee));
            await Client.ResourceOwnerApproveAsync(TestDepartmentId, request.Id);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            var result = await Client.TestClientGetAsync<TestSecondOpinionResult>($"/resources/requests/internal/{request.Id}/second-opinions/");
            var prompt = result.Value.Value.First();

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

            var result = await Client.TestClientGetAsync<TestSecondOpinionResult>(endpoint);
            result.Value.Value.Should().Contain(x => x.Id == secondOpinion.Id);
        }

        [Fact]
        public async Task GetPersonalSecondOpinions_ShouldFilterByRequestState()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var endpoint = $"/persons/me/second-opinions/?$filter=state eq 'Active'";
            var secondOpinion = await CreateSecondOpinion(request, testUser);

            var result = await Client.TestClientGetAsync<TestSecondOpinionResult>(endpoint);
            result.Value.Value.Should().Contain(x => x.Id == secondOpinion.Id);

            await Client.AssignDepartmentAsync(request.Id, TestDepartmentId);
            await Client.ProposePersonAsync(request.Id, fixture.AddProfile(FusionAccountType.Employee));
            await Client.ResourceOwnerApproveAsync(TestDepartmentId, request.Id);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            result = await Client.TestClientGetAsync<TestSecondOpinionResult>(endpoint);
            result.Value.Value.Should().NotContain(x => x.Id == secondOpinion.Id);
        }

        [Fact]
        public async Task OptionsOnSecondOpinion_Should_AllowGetAndPost()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var secondOpinion = await CreateSecondOpinion(request, testUser);

            var optionsResult = await Client.TestClientOptionsAsync($"/resources/requests/internal/{request.Id}/second-opinions/");
            var allowed = optionsResult.Response.Content.Headers.Allow;
            allowed.Should().Contain("POST");
            allowed.Should().Contain("GET");
        }

        [Fact]
        public async Task OptionsOnSecondOpinion_ShouldNot_AllowPost_WhenRequestIsCompleted()
        {
            using var adminScope = fixture.AdminScope();
            var department = "PDP PRD FE ANE";
            fixture.EnsureDepartment(department);

            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var secondOpinion = await CreateSecondOpinion(request, testUser);

            await Client.AssignDepartmentAsync(request.Id, department);
            await Client.ProposePersonAsync(request.Id, fixture.AddProfile(FusionAccountType.Employee));
            await Client.ResourceOwnerApproveAsync(department, request.Id);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            var optionsResult = await Client.TestClientOptionsAsync($"/resources/requests/internal/{request.Id}/second-opinions/");
            var allowed = optionsResult.Response.Content.Headers.Allow;
            allowed.Should().NotContain("POST");
        }

        [Fact]
        public async Task DeleteSecondOpinion_ShouldSucceed_WhenItHasResponses()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var secondOpinion = await CreateSecondOpinion(request, testUser);
            await AddResponse(request.Id, secondOpinion.Id, secondOpinion.Responses.Single().Id);

            var result = await Client.TestClientDeleteAsync($"/resources/requests/internal/{request.Id}/second-opinions/{secondOpinion.Id}");
            result.Should().BeSuccessfull();
        }

        [Fact]
        public async Task DeleteSecondOpinion_ShouldFail_WhenRequestIsCompleted()
        {
            var department = "PDP PRD FE ANE";
            fixture.EnsureDepartment(department);

            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var secondOpinion = await CreateSecondOpinion(request, testUser);
            await AddResponse(request.Id, secondOpinion.Id, secondOpinion.Responses.Single().Id);

            await Client.AssignDepartmentAsync(request.Id, department);
            await Client.ProposePersonAsync(request.Id, fixture.AddProfile(FusionAccountType.Employee));
            await Client.ResourceOwnerApproveAsync(department, request.Id);

            await Client.TaskOwnerApproveAsync(testProject, request.Id);
            await Client.ProvisionRequestAsync(request.Id);

            var result = await Client.TestClientDeleteAsync($"/resources/requests/internal/{request.Id}/second-opinions/{secondOpinion.Id}");
            result.Should().BeBadRequest();
        }

        [Fact]
        public async Task DeleteSecondOpinionResponse_ShouldSucceed()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var secondOpinion = await CreateSecondOpinion(request, testUser);
            var response = secondOpinion.Responses.Single();

            await AddResponse(request.Id, secondOpinion.Id, response.Id);

            var endpoint = $"/resources/requests/internal/{request.Id}/second-opinions/{secondOpinion.Id}/responses/{response.Id}";
            var result = await Client.TestClientDeleteAsync(endpoint);
            result.Should().BeSuccessfull();
        }

        [Fact]
        public async Task DeletingRequest_ShouldSucceed_WhenHasSecondOpinion()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);

            var secondOpinion = await CreateSecondOpinion(request, testUser);

            var endpoint = $"/resources/requests/internal/{request.Id}";
            var result = await Client.TestClientDeleteAsync(endpoint);
            result.Should().BeSuccessfull();
        }

        [Fact]
        public async Task ShouldCountSecondOpinionResponses()
        {
            using var adminScope = fixture.AdminScope();
            const string department = "TST ABC DEF";

            fixture.EnsureDepartment(department);

            var users = new List<ApiPersonProfileV3> { testUser };
            for (int i = 0; i < 5; i++)
            {
                users.Add(fixture.AddProfile(FusionAccountType.Employee));
            }

            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);
            await Client.AssignDepartmentAsync(request.Id, department);

            var secondOpinion = await CreateSecondOpinion(request, users.ToArray());
            
            var response = secondOpinion.Responses.Single(x => x.AssignedTo.AzureUniquePersonId == testUser.AzureUniqueId);
            await AddResponse(request.Id, secondOpinion.Id, response.Id);

            var result = await Client.TestClientGetAsync($"departments/{department}/resources/requests", new
            {
                value = new[]
                {
                    new
                    {
                        id = Guid.NewGuid(),
                        secondOpinionCounts = new { totalCount = 0, publishedCount = 0 }
                    }
                }
            });

            var counts = result.Value.value.Single(x => x.id == request.Id).secondOpinionCounts;
            counts.totalCount.Should().Be(6);
            counts.publishedCount.Should().Be(1);
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

        private async Task AddResponse(Guid requestId, Guid secondOpinionId, Guid responseId, ApiPersonProfileV3 user = null, string state = "Published", string comment = "This is my comment")
        {
            var payload = new
            {
                Comment = comment,
                State = state
            };

            using var userScope = fixture.UserScope(user ?? testUser);
            var patchResult = await Client.TestClientPatchAsync<TestSecondOpinionPrompt>($"/resources/requests/internal/{requestId}/second-opinions/{secondOpinionId}/responses/{responseId}", payload);
            patchResult.Should().BeSuccessfull();
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
