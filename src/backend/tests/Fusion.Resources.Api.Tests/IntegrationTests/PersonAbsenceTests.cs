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
using Fusion.Testing.Authentication.User;
using Xunit;
using Xunit.Abstractions;
using Fusion.Testing.Mocks.OrgService;
#nullable enable
namespace Fusion.Resources.Api.Tests.IntegrationTests
{

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
            testUser.FullDepartment = "TPD PRD FE MMC EAM";
            testUser.Department = "FE MMC EAM";
        }

        [Fact]
        public async Task OptionsAbsence_GetAllowedForPerson_WhenCurrentUser()
        {
            using var userScope = fixture.UserScope(testUser);
            var result = await client.TestClientOptionsAsync($"/persons/{testUser.AzureUniqueId}/absence");
            result.Should().BeSuccessfull();
            CheckAllowHeader("GET", result);
        }
        [Fact]
        public async Task OptionsAbsence_GetNotAllowedForPerson_WhenOtherUser()
        {
            var otherUser = fixture.AddProfile(FusionAccountType.Employee);
            using var userScope = fixture.UserScope(otherUser);
            var result = await client.TestClientOptionsAsync($"/persons/{testUser.AzureUniqueId}/absence");
            result.Should().BeSuccessfull();
            CheckAllowHeader("!GET", result);
        }

        [Fact]
        public async Task OptionsAbsenceItem_GetAllowedForPerson_WhenCurrentUser()
        {
            using var userScope = fixture.UserScope(testUser);
            var result = await client.TestClientOptionsAsync($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}");
            result.Should().BeSuccessfull();
            CheckAllowHeader("GET", result);
        }
        [Fact]
        public async Task OptionsAbsenceItem_GetNotAllowedForPerson_WhenOtherUser()
        {
            var otherUser = fixture.AddProfile(FusionAccountType.Employee);
            using var userScope = fixture.UserScope(otherUser);
            var result = await client.TestClientOptionsAsync($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}");
            result.Should().BeSuccessfull();
            CheckAllowHeader("!GET", result);
        }
        
        [Fact]
        public async Task GetAbsenceForUser_ShouldBeOk_WhenCurrentUser()
        {
            using var testScope = fixture.UserScope(testUser);
            var response = await client.TestClientGetAsync($"/persons/{testUser.AzureUniqueId}/absence", new { value = new[] { new { id = Guid.Empty } } });
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task GetAbsenceForUser_ShouldBeUnauthorized_WhenOtherUser()
        {
            var otherUser = fixture.AddProfile(FusionAccountType.Employee);
            using var testScope = fixture.UserScope(otherUser);
            var response = await client.TestClientGetAsync($"/persons/{testUser.AzureUniqueId}/absence", new { value = new[] { new { id = Guid.Empty } } });
            response.Should().BeUnauthorized();
        }

        [Fact]
        public async Task GetAbsenceItem_ShouldBeOk_WhenCurrentUser()
        {
            using var testScope = fixture.UserScope(testUser);
            var response = await client.TestClientGetAsync($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}", new { value = new[] { new { id = Guid.Empty } } });
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task GetAbsenceItemForUser_ShouldBeUnauthorized_WhenOtherUser()
        {
            var otherUser = fixture.AddProfile(FusionAccountType.Employee);
            using var testScope = fixture.UserScope(otherUser);
            var response = await client.TestClientGetAsync($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}", new { value = new[] { new { id = Guid.Empty } } });
            response.Should().BeUnauthorized();
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
                AppliesFrom = new DateTime(2021, 04, 30),
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
            response.Value.Type.Should().Be(request.Type.ToString());
            response.Value.AbsencePercentage.Should().BeNull();

        }

        [Fact]
        public async Task DeleteAbsence_ShouldBeOk_WhenAdmin()
        {
            using var authScope = fixture.AdminScope();
            var response = await client.TestClientDeleteAsync($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}");
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task CreateAbsence_ShouldUseBasePositionName_WhenRoleNameNull()
        {
            const string BasePositionName = "Base position name";
            var testProject = new FusionTestProjectBuilder();
            var basePosition = testProject
                .AddBasePosition(BasePositionName);
            OrgServiceMock.AddProject(testProject);

            var request = new CreatePersonAbsenceRequest
            {
                AppliesFrom = new DateTime(2021, 04, 30),
                AppliesTo = new DateTime(2022, 04, 30),
                Comment = "A comment",
                Type = ApiPersonAbsence.ApiAbsenceType.OtherTasks,
                AbsencePercentage = null,
                IsPrivate = false,
                TaskDetails = new ApiTaskDetails
                {
                    BasePositionId = basePosition.Id,
                    TaskName = "task name",
                    RoleName = null,
                    Location = "location"
                }
            };

            using var authScope = fixture.AdminScope();
            var response = await client.TestClientPutAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}", request);
            response.Should().BeSuccessfull();

            response.Value.TaskDetails?.RoleName.Should().Be(BasePositionName);
        }

        [Fact]
        public async Task GetAbsence_ShouldBeHiddenForOtherResourceOwners_WhenPrivate()
        {
            var siblingResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            siblingResourceOwner.FullDepartment = "TPD PRD TST QWE ABC";
            siblingResourceOwner.Department = "TST QWE ABC";
            siblingResourceOwner.IsResourceOwner = true;

            var request = new CreatePersonAbsenceRequest
            {
                AppliesFrom = new DateTime(2021, 04, 30),
                AppliesTo = new DateTime(2022, 04, 30),
                Comment = "A comment",
                Type = ApiPersonAbsence.ApiAbsenceType.OtherTasks,
                AbsencePercentage = null,
                IsPrivate = true,

                TaskDetails = new ApiTaskDetails
                {
                    TaskName = "Top secret task name",
                    RoleName = "Top secret role name",
                    Location = "Top secret location"
                }
            };

            Guid absenceId;
            using var authScope = fixture.AdminScope();
            {
                var response = await client.TestClientPostAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence/", request);
                absenceId = response.Value.Id;
                response.Should().BeSuccessfull();
            }

            using var userScope = fixture.UserScope(siblingResourceOwner);
            {
                var response = await client.TestClientGetAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence/{absenceId}");
                response.Should().BeSuccessfull();

                var taskDetails = response.Value.TaskDetails;
                taskDetails.Should().NotBeNull();

                taskDetails!.IsHidden.Should().Be(true);

                response.Value.Comment.Should().NotBe(request.Comment);
                taskDetails!.TaskName.Should().NotBe(request.TaskDetails.TaskName);
                taskDetails!.RoleName.Should().NotBe(request.TaskDetails.RoleName);
                taskDetails!.Location.Should().NotBe(request.TaskDetails.Location);
            }
        }

        [Theory]
        [InlineData(ApiPersonAbsence.ApiAbsenceType.Absence)]
        [InlineData(ApiPersonAbsence.ApiAbsenceType.Vacation)]
        public async Task CreateAbsence_ShouldFail_WhenSettingTaskDetailsOnType(ApiPersonAbsence.ApiAbsenceType type)
        {
            var request = new CreatePersonAbsenceRequest
            {
                AppliesFrom = new DateTime(2021, 04, 30),
                AppliesTo = new DateTime(2022, 04, 30),
                Comment = "A comment",
                Type = type,
                AbsencePercentage = null,
                IsPrivate = true,

                TaskDetails = new ApiTaskDetails
                {
                    TaskName = "Top secret task name",
                    RoleName = "Top secret role name",
                    Location = "Top secret location"
                }
            };

            using var authScope = fixture.AdminScope();
            var response = await client.TestClientPostAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence/", request);
            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task AddTaskWithBasePosition_ShouldBeAllowed()
        {
            var task = new CreatePersonAbsenceRequest
            {
                AbsencePercentage = 50,
                AppliesFrom = new DateTime(2021, 08, 12),
                AppliesTo = new DateTime(2021, 09, 03),
                Comment = "",
                IsPrivate = false,
                TaskDetails = new ApiTaskDetails
                {
                    BasePositionId = new Guid("b97703e6-cdc8-4a3f-a889-21a1d375422f"),
                    Location = "",
                    TaskName = "Test"
                },
                Type = ApiPersonAbsence.ApiAbsenceType.OtherTasks
            };

            using var authScope = fixture.AdminScope();
            var response = await client.TestClientPostAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence/", task);
            response.Should().BeSuccessfull();
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        public async Task AddTaskWithoutBasePositionOrRoleName_ShouldNotBeAllowed(string method)
        {
            var task = new CreatePersonAbsenceRequest
            {
                AbsencePercentage = 50,
                AppliesFrom = new DateTime(2021, 08, 12),
                AppliesTo = new DateTime(2021, 09, 03),
                Comment = "",
                IsPrivate = false,
                TaskDetails = new ApiTaskDetails
                {
                    Location = "",
                    TaskName = "Test"
                },
                Type = ApiPersonAbsence.ApiAbsenceType.OtherTasks
            };

            using var authScope = fixture.AdminScope();

            var response = method switch
            {
                "POST" => await client.TestClientPostAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence/", task),
                "PUT" => await client.TestClientPutAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence/{this.TestAbsenceId}", task),
                _ => null
            };

            response.Should().BeBadRequest();
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

        private static void CheckAllowHeader(string allowed, TestClientHttpResponse<dynamic> result)
        {
            var expectedVerbs = allowed
                .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.StartsWith('!') ? new { Key = "disallowed", Method = new HttpMethod(x.Substring(1)) } : new { Key = "allowed", Method = new HttpMethod(x) })
                .ToLookup(x => x.Key, x => x.Method);

            if (expectedVerbs["allowed"].Any())
                result.Should().HaveAllowHeaders(expectedVerbs["allowed"].ToArray());

            if (expectedVerbs["disallowed"].Any())
                result.Should().NotHaveAllowHeaders(expectedVerbs["disallowed"].ToArray());
        }
    }

    public class TestAbsence
    {
        public Guid Id { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset? AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public string? Type { get; set; }
        public double? AbsencePercentage { get; set; }
        public bool IsPrivate { get; set; } = false;

        public TestTaskDetails? TaskDetails { get; set; }
    }
    public class TestTaskDetails
    {
        public bool IsHidden { get; set; }
        public string? RoleName { get; set; }
        public string? TaskName { get; set; }
        public string? Location { get; set; }
    }
}
