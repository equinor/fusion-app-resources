using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Domain;
using Fusion.Testing.Authentication.User;
using Xunit;
using Xunit.Abstractions;
#nullable enable
namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class PersonNotesTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;
        /// <summary>
        /// Will be generated new for each test
        /// </summary>
        private readonly ApiPersonProfileV3 testUser;

        private Guid testNoteId;

        private HttpClient client => fixture.ApiFactory.CreateClient();

        public PersonNotesTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.Employee);
        }

        [Fact]
        public async Task Create_ShouldBeSuccessful_WhenUserExists()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await client.TestClientPostAsync($"persons/{testUser.AzureUniqueId}/resources/notes?api-version=1.0-preview", new
            {
                title = $"Test {Guid.NewGuid()}",
                content = "My test note"
            }, new { id = Guid.Empty, title = string.Empty, content = string.Empty, isShared = false });
            resp.Should().BeSuccessfull();

            resp.Value.id.Should().NotBeEmpty();
            resp.Value.title.Should().NotBeNullOrEmpty();
            resp.Value.content.Should().NotBeNullOrEmpty();
            resp.Value.isShared.Should().BeFalse();
        }

        [Fact]
        public async Task Delete_ShouldBeSuccessfull()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await client.TestClientDeleteAsync($"persons/{testUser.AzureUniqueId}/resources/notes/{testNoteId}?api-version=1.0-preview");
            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task Update_ShouldBeSuccessfull()
        {
            var newTitle = $"Updated title {Guid.NewGuid()}";
            var newContent = $"My new content {Guid.NewGuid()}";

            using var adminScope = fixture.AdminScope();
            var resp = await client.TestClientPutAsync($"persons/{testUser.AzureUniqueId}/resources/notes/{testNoteId}?api-version=1.0-preview", new
            {
                title = newTitle,
                content = newContent,
                isShared = true
            }, new { id = Guid.Empty, title = string.Empty, content = string.Empty, isShared = false });

            resp.Should().BeSuccessfull();

            resp.Value.title.Should().Be(newTitle);
            resp.Value.content.Should().Be(newContent);
            resp.Value.isShared.Should().Be(true);
        }

        public async Task InitializeAsync()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await client.TestClientPostAsync($"persons/{testUser.AzureUniqueId}/resources/notes?api-version=1.0-preview", new
            {
                title = $"Test {Guid.NewGuid()}",
                content = "My test note"
            }, new { id = Guid.Empty });
            resp.Should().BeSuccessfull();

            testNoteId = resp.Value.id;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }

    public class PersonAbsenceTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;
        /// <summary>
        /// Will be generated new for each test
        /// </summary>
        private readonly ApiPersonProfileV3 testUser;

        private Guid TestAbsenceId;

        private HttpClient client => fixture.ApiFactory.CreateClient();

        public PersonAbsenceTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.Employee);
        }

        [Fact]
        public async Task ListAbsence_ShouldBeOk_WhenAdmin()
        {
            using var adminScope = fixture.AdminScope();
            var response = await client.TestClientGetAsync($"/persons/{testUser.AzureUniqueId}/absence", new { value = new[] { new { id = Guid.Empty } } });
            response.Should().BeSuccessfull();

            response.Value.value.Count().Should().BeGreaterOrEqualTo(1);
        }

        [Fact]
        public async Task GetAbsence_ShouldBeOk_WhenAdmin()
        {
            using var authScope = fixture.AdminScope();
            var response = await client.TestClientGetAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}");
            response.Should().BeSuccessfull();

            response.Value.Id.Should().NotBeEmpty();
            response.Value.AppliesFrom.Should().NotBeNull();
            response.Value.AppliesTo.Should().NotBeNull();
            response.Value.Comment.Should().NotBeNullOrEmpty();
            response.Value.Type.Should().NotBeNull();
            response.Value.AbsencePercentage.Should().NotBeNull();
        }

        [Fact]
        public async Task PutAbsence_ShouldBeOk_WhenAdmin()
        {
            var request = new CreatePersonAbsenceRequest
            {
                AppliesFrom = new DateTime(2021,04,30),
                AppliesTo = new DateTime(2022, 04, 30),
                Comment = "A comment",
                Type = ApiPersonAbsence.ApiAbsenceType.Vacation,
                AbsencePercentage = null // Clearing absencePercentage = 100% 
            };

            using var authScope = fixture.AdminScope();
            var response = await client.TestClientPutAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}", request);
            response.Should().BeSuccessfull();

            response.Value.Id.Should().Be(TestAbsenceId);
            response.Value.AppliesFrom.Should().Be(request.AppliesFrom);
            response.Value.AppliesTo.Should().Be(request.AppliesTo);
            response.Value.Comment.Should().Be(request.Comment);
            response.Value.Type.Should().Be(request.Type);
            response.Value.AbsencePercentage.Should().BeNull();

        }

        [Fact]
        public async Task DeleteAbsence_ShouldBeOk_WhenAdmin()
        {
            using var authScope = fixture.AdminScope();
            var response = await client.TestClientDeleteAsync($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}");
            response.Should().BeSuccessfull();
        }


        public async Task InitializeAsync()
        {
            var client = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            var request = new CreatePersonAbsenceRequest
            {
                AppliesFrom = new DateTime(2021, 04, 30),
                AppliesTo = new DateTime(2022, 04, 30),
                Comment = "A comment",
                Type = ApiPersonAbsence.ApiAbsenceType.Absence,
                AbsencePercentage = 100
            };

            var response = await client.TestClientPostAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence", request);

            TestAbsenceId = response.Value.Id;
        }


        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
    }

    public class TestAbsence
    {
        public Guid Id { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset? AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public QueryAbsenceType? Type { get; set; }
        public double? AbsencePercentage { get; set; }
    }
}
