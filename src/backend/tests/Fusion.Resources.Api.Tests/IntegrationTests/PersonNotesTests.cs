using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Domain;
using Fusion.Testing;
using System;
using System.Net.Http;
using System.Threading.Tasks;
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
        private readonly ApiPersonProfileV3 resourceOwner;

        private Guid testNoteId;

        private HttpClient client => fixture.ApiFactory.CreateClient();

        public PersonNotesTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.Employee);
            testUser.FullDepartment = "L1 L2 L3 L4";
            testUser.Department = "L2 L3 L4";

            resourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            resourceOwner.FullDepartment = testUser.FullDepartment;
            resourceOwner.IsResourceOwner = true;
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
        public async Task Create_ShouldBeSuccessful_WhenResourceOwner()
        {
            using var resOwnerScope = fixture.UserScope(resourceOwner);
            var resp = await client.TestClientPostAsync($"persons/{testUser.AzureUniqueId}/resources/notes?api-version=1.0-preview", new
            {
                title = $"Test {Guid.NewGuid()}",
                content = "My test note"
            }, new { });
            
            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task Create_ShouldBeUnauthorized_WhenResourceOwnerInOtherDepartment()
        {
            resourceOwner.FullDepartment = "L1 L2 L3 L4A";

            using var resOwnerScope = fixture.UserScope(resourceOwner);
            var resp = await client.TestClientPostAsync($"persons/{testUser.AzureUniqueId}/resources/notes?api-version=1.0-preview", new
            {
                title = $"Test {Guid.NewGuid()}",
                content = "My test note"
            }, new { });

            resp.Should().BeUnauthorized();
        }

        [Fact]
        public async Task Delete_ShouldBeSuccessfull()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await client.TestClientDeleteAsync($"persons/{testUser.AzureUniqueId}/resources/notes/{testNoteId}?api-version=1.0-preview");
            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task Delete_ShouldBeSuccessfull_WhenResourceOwner()
        {
            using var resOwnerScope = fixture.UserScope(resourceOwner);
            var resp = await client.TestClientDeleteAsync($"persons/{testUser.AzureUniqueId}/resources/notes/{testNoteId}?api-version=1.0-preview");
            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task Delete_ShouldBeUnauthorizer_WhenResourceOwnerInOtherDepartment()
        {
            resourceOwner.FullDepartment = "L1 L2 L3 L4A";
            using var resOwnerScope = fixture.UserScope(resourceOwner);

            var resp = await client.TestClientDeleteAsync($"persons/{testUser.AzureUniqueId}/resources/notes/{testNoteId}?api-version=1.0-preview");
            resp.Should().BeUnauthorized();
        }

        [Fact]
        public async Task Update_ShouldBeSuccessfull_WhenResourceOwner()
        {
            using var resOwnerScope = fixture.UserScope(resourceOwner);
            var resp = await client.TestClientPutAsync($"persons/{testUser.AzureUniqueId}/resources/notes/{testNoteId}?api-version=1.0-preview", new
            {
                title = $"Updated title {Guid.NewGuid()}",
                content = $"My new content {Guid.NewGuid()}",
                isShared = true
            }, new { });

            resp.Should().BeSuccessfull();
        }

        [Theory]
        [InlineData("L1 L2 ")]
        [InlineData("L1 L2 L3")]
        [InlineData("L1 L2 L3 L4A")]
        [InlineData("L1 L2 L3A L4")]
        public async Task Update_ShouldBeUnauthorized_WhenOtherResourceOwnerIn(string department)
        {
            resourceOwner.FullDepartment = department;

            using var resOwnerScope = fixture.UserScope(resourceOwner);
            var resp = await client.TestClientPutAsync($"persons/{testUser.AzureUniqueId}/resources/notes/{testNoteId}?api-version=1.0-preview", new
            {
                title = $"Updated title {Guid.NewGuid()}",
                content = $"{Guid.NewGuid()}",
                isShared = true
            }, new { });

            resp.Should().BeUnauthorized();
        }

        [Fact]
        public async Task GetNotes_ShouldOnlyBeVisibleToDepartmentOwner_WhenNotShared()
        {
            var privateNote = await CreateNoteAsAdminAsync(client, isShared: false);

            using var resOwnerScope = fixture.UserScope(resourceOwner);

            var resp = await client.TestClientGetAsync($"persons/{testUser.AzureUniqueId}/resources/notes?api-version=1.0-preview", new[] { new { id = Guid.Empty } });
            resp.Should().BeSuccessfull();

            resp.Value.Should().Contain(n => n.id == privateNote);
        }

        [Fact]
        public async Task GetNotes_ShouldOnlyDisplaySharedNotes_WhenParentResourceOwner()
        {
            var privateNote = await CreateNoteAsAdminAsync(client, isShared: true);

            // Update the department for the resource owner
            resourceOwner.FullDepartment = new DepartmentPath(testUser.FullDepartment!).Parent();

            using var resOwnerScope = fixture.UserScope(resourceOwner);

            var resp = await client.TestClientGetAsync($"persons/{testUser.AzureUniqueId}/resources/notes?api-version=1.0-preview", new[] { new { id = Guid.Empty } });
            resp.Should().BeSuccessfull();

            resp.Value.Should().Contain(n => n.id == privateNote);
        }

        [Theory]
        [InlineData("L1 L2 L3 L4A", true)]
        [InlineData("L1 L2 L3A L4A", true)]
        [InlineData("L1 L2A L3A L4A", false)]
        public async Task GetNotes_ShouldOnlyDisplaySharedNotes_WhenSiblingResourceOwner(string departmentPath, bool success)
        {
            var privateNote = await CreateNoteAsAdminAsync(client, isShared: true);

            // Update the department for the resource owner
            resourceOwner.FullDepartment = departmentPath;

            using var resOwnerScope = fixture.UserScope(resourceOwner);

            var resp = await client.TestClientGetAsync($"persons/{testUser.AzureUniqueId}/resources/notes?api-version=1.0-preview", new[] { new { id = Guid.Empty } });

            if (success)
            {
                resp.Should().BeSuccessfull();
                resp.Value.Should().Contain(n => n.id == privateNote);
            }
            else
                resp.Should().BeUnauthorized();
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


        private async Task<Guid> CreateNoteAsAdminAsync(HttpClient client, string? title = null, bool isShared = false)
        {
            using var adminScope = fixture.AdminScope();

            var resp = await client.TestClientPutAsync($"persons/{testUser.AzureUniqueId}/resources/notes/{testNoteId}?api-version=1.0-preview", new
            {
                title = title ?? $"Test note {Guid.NewGuid()}",
                content = $"New content {Guid.NewGuid()}",
                isShared = isShared
            }, new { id = Guid.Empty, title = string.Empty, content = string.Empty, isShared = false });

            return resp.Value.id;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
