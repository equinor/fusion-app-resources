using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Api.Tests.IntegrationTests;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.AuthorizationTests
{
    public class SecurityMatrixTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        const string TestDepartment = "TPD PRD TST ASD";
        const string SiblingDepartment = "TPD PRD TST FGH";
        const string ParentDepartment = "TPD PRD TST";

        const string SameL2Department = "TPD PRD FE MMS STR1";

        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private FusionTestProjectBuilder testProject;
        private ApiClients.Org.ApiPositionV2 testPosition;
        private OrgRequestInterceptor creatorInterceptor;

        public Dictionary<string, ApiPersonProfileV3> Users { get; private set; }

        private ApiPersonProfileV3 testUser;

        public SecurityMatrixTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            fixture.DisableMemoryCache();

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            var creator = fixture.AddProfile(FusionAccountType.Employee);
            var resourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            resourceOwner.IsResourceOwner = true;

            var resourceOwnerCreator = fixture.AddProfile(FusionAccountType.Employee);
            resourceOwnerCreator.IsResourceOwner = true;
            resourceOwnerCreator.FullDepartment = TestDepartment;

            testProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .WithProperty("pimsWriteSyncEnabled", true)
                .AddToMockService();

            fixture.ContextResolver
               .AddContext(testProject.Project);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = TestDepartment);
            testPosition = testProject.AddPosition()
                .WithBasePosition(bp)
                .WithAssignedPerson(fixture.AddProfile(FusionAccountType.Employee))
                .WithEnsuredFutureInstances();

            Users = new Dictionary<string, ApiPersonProfileV3>()
            {
                ["creator"] = creator,
                ["resourceOwner"] = resourceOwner,
                ["resourceOwnerCreator"] = resourceOwnerCreator
            };

            testUser = fixture.AddProfile(FusionAccountType.Employee);
            testUser.FullDepartment = TestDepartment;
        }

        public Task InitializeAsync() => Task.CompletedTask;

        [Theory]
        [InlineData("resourceOwner", TestDepartment, false)]
        [InlineData("resourceOwner", SiblingDepartment, false)]
        [InlineData("resourceOwner", ParentDepartment, false)]
        [InlineData("resourceOwner", SameL2Department, false)]
        [InlineData("creator", "TPD RND WQE FQE", true)]
        public async Task CanDeleteRequestAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientDeleteAsync<dynamic>($"/projects/{testProject.Project.ProjectId}/requests/{request.Id}");

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", true)]

        public async Task CanReadRequestsAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientGetAsync<TestApiInternalRequestModel>($"/projects/{testProject.Project.ProjectId}/requests/{request.Id}");

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", true)]

        public async Task CanEditGeneralOnRequestAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/projects/{testProject.Project.ProjectId}/requests/{request.Id}",
                new
                {
                    proposedChanges = new
                    {
                        location = new { id = Guid.NewGuid(), name = "Test location" }
                    }
                }
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, false)]
        [InlineData("resourceOwner", SiblingDepartment, false)]
        [InlineData("resourceOwner", ParentDepartment, false)]
        [InlineData("resourceOwner", SameL2Department, false)]
        [InlineData("creator", "TPD RND WQE FQE", true)]
        public async Task CanEditAdditionalCommentOnRequestAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/projects/{testProject.Project.ProjectId}/requests/{request.Id}",
                new
                {
                    additionalNote = "updated comment"
                }
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", true)]
        public async Task CanReassignOnRequestAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            const string changedDepartment = "TPD UPD ASD";
            fixture.EnsureDepartment(changedDepartment);

            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/projects/{testProject.Project.ProjectId}/requests/{request.Id}",
                new { assignedDepartment = changedDepartment }
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, false)]
        public async Task CanCreateResourceOwnerRequest(string role, string department, bool shouldBeAllowed)
        {
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            using var i = creatorInterceptor = OrgRequestMocker
                 .InterceptOption($"/{testPosition.Id}")
                 .RespondWithHeaders(HttpStatusCode.NoContent, h => h.Add("Allow", "PUT"));
            
            var client = fixture.ApiFactory.CreateClient();
            
            var result = await client.TestClientPostAsync<TestApiInternalRequestModel>(
                $"/departments/{TestDepartment}/resources/requests",
                new ApiCreateInternalRequestModel()
                    .AsTypeResourceOwner("changeResource")
                    .WithPosition(testPosition)
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, false)]
        [InlineData("resourceOwner", SiblingDepartment, false)]
        [InlineData("resourceOwner", ParentDepartment, false)]
        [InlineData("resourceOwner", SameL2Department, false)]
        [InlineData("creator", "TPD RND WQE FQE", true)]
        public async Task CanStartNormalRequest(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPostAsync<dynamic>(
                $"/projects/{testProject.Project.ProjectId}/requests/{request.Id}/start", null
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", true)]
        public async Task CanProposeNormalRequest(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var proposedPerson = PeopleServiceMock.AddTestProfile()
                .SaveProfile();

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/resources/requests/internal/{request.Id}",
                new { proposedPersonAzureUniqueId = proposedPerson.AzureUniqueId });

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, false)]
        [InlineData("resourceOwner", SiblingDepartment, false)]
        [InlineData("resourceOwner", ParentDepartment, false)]
        [InlineData("resourceOwner", SameL2Department, false)]
        //TODO: [InlineData("creator", "TPD RND WQE FQE", true)]
        public async Task CanAcceptNormalRequest(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;

            using (var adminScope = fixture.AdminScope())
            {
                var proposedPerson = PeopleServiceMock.AddTestProfile()
                    .SaveProfile();

                var adminClient = fixture.ApiFactory.CreateClient();
                await adminClient.ProposePersonAsync(request.Id, proposedPerson);
            }

            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPostAsync<TestApiInternalRequestModel>(
                $"/projects/{testProject.Project.ProjectId}/resources/requests/{request.Id}/approve",
                null
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("resourceOwnerCreator", TestDepartment, true)]
        public async Task CanStartChangeRequest(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateChangeRequest(TestDepartment);

            var client = fixture.ApiFactory.CreateClient();
            using (var adminscope = fixture.AdminScope())
            {
                var testUser = fixture.AddProfile(FusionAccountType.Employee);

                await client.SetChangeParamsAsync(request.Id, DateTime.Today.AddDays(1));
                await client.ProposePersonAsync(request.Id, testUser);
            }
            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var result = await client.TestClientPostAsync<TestApiInternalRequestModel>(
                $"/departments/{department}/resources/requests/{request.Id}/start",
                null
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        //[Theory]
        //[InlineData("resourceOwnerCreator", TestDepartment, false)]
        //public async Task CanAcceptChangeRequest(string role, string department, bool shouldBeAllowed)
        //{
        //    var chgRequest = await CreateChangeRequest(department);
        //    using (var adminScope = fixture.AdminScope())
        //    {
        //        var adminClient = fixture.ApiFactory.CreateClient();
        //        await adminClient.TestClientPostAsync<TestApiInternalRequestModel>(
        //            $"/departments/{TestDepartment}/resources/requests/{chgRequest.Id}/start",
        //            null
        //        );
        //    }
        //    using var userScope = fixture.UserScope(Users[role]);

        //    Users[role].FullDepartment = department;
        //    var client = fixture.ApiFactory.CreateClient();
        //    var result = await client.TestClientPostAsync<TestApiInternalRequestModel>(
        //        $"/departments/{chgRequest.AssignedDepartment}/requests/{chgRequest.Id}/approve",
        //        null
        //    );

        //    if (shouldBeAllowed) result.Should().BeSuccessfull();
        //    else result.Should().BeUnauthorized();
        //}

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, false)]

        public async Task CanAddPersonAbsence(string role, string department, bool shouldBeAllowed)
        {
            var testUser = fixture.AddProfile(FusionAccountType.Employee);
            testUser.FullDepartment = TestDepartment;

            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientPostAsync<TestAbsence>(
                $"/persons/{testUser.AzureUniqueId}/absence",
                new CreatePersonAbsenceRequest
                {
                    AppliesFrom = new DateTime(2021, 04, 30),
                    AppliesTo = new DateTime(2022, 04, 30),
                    Comment = "A comment",
                    Type = ApiPersonAbsence.ApiAbsenceType.Absence,
                    AbsencePercentage = 100
                }
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, false)]
        public async Task CanEditPersonAbsence(string role, string department, bool shouldBeAllowed)
        {
            var absence = await CreateAbsence();

            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientPutAsync<dynamic>(
                $"/persons/{testUser.AzureUniqueId}/absence/{absence.Id}",
                new UpdatePersonAbsenceRequest
                {
                    AppliesFrom = new DateTime(2021, 04, 30),
                    AppliesTo = new DateTime(2022, 04, 30),
                    Comment = "An updated comment",
                    Type = ApiPersonAbsence.ApiAbsenceType.Absence,
                    AbsencePercentage = 50
                }
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, false)]
        public async Task CanDeletePersonAbsence(string role, string department, bool shouldBeAllowed)
        {
            var absence = await CreateAbsence();

            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientDeleteAsync<dynamic>(
                $"/persons/{testUser.AzureUniqueId}/absence/{absence.Id}"
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, false)]
        public async Task CanGetPersonAbsence(string role, string department, bool shouldBeAllowed)
        {
            var absence = await CreateAbsence();

            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientGetAsync<TestAbsence>(
                $"/persons/{testUser.AzureUniqueId}/absence/{absence.Id}"
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, false)]
        public async Task CanGetAllAbsenceForPerson(string role, string department, bool shouldBeAllowed)
        {
            var absence = await CreateAbsence();

            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientGetAsync<dynamic>(
                $"/persons/{testUser.AzureUniqueId}/absence/"
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, "GET,POST")]
        [InlineData("resourceOwner", SiblingDepartment, "GET,POST")]
        [InlineData("resourceOwner", ParentDepartment, "GET,POST")]
        [InlineData("resourceOwner", SameL2Department, "!GET,!POST")]
        public async Task CanGetAbsenceOptionsForPerson(string role, string department, string allowed)
        {
            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientOptionsAsync(
                $"/persons/{testUser.AzureUniqueId}/absence"
            );

            CheckHeaders(allowed, result);
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, "GET,PUT,DELETE")]
        [InlineData("resourceOwner", SiblingDepartment, "GET,PUT,DELETE")]
        [InlineData("resourceOwner", ParentDepartment, "GET,PUT,DELETE")]
        [InlineData("resourceOwner", SameL2Department, "!GET,!PUT,!DELETE")]
        public async Task CanGetAbsenceOptions(string role, string department, string allowed)
        {
            var absence = await CreateAbsence();

            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientOptionsAsync(
                $"/persons/{testUser.AzureUniqueId}/absence/{absence.Id}"
            );

            CheckHeaders(allowed, result);
        }

        private static void CheckHeaders(string allowed, TestClientHttpResponse<dynamic> result)
        {
            var expectedVerbs = allowed
                            .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(x =>
                            {
                                if (x.StartsWith('!'))
                                    return new { Key = "disallowed", Method = new HttpMethod(x.Substring(1)) };
                                else
                                    return new { Key = "allowed", Method = new HttpMethod(x) };
                            })
                            .ToLookup(x => x.Key, x => x.Method);

            if (expectedVerbs["allowed"].Any())
                result.Should().HaveAllowHeaders(expectedVerbs["allowed"].ToArray());

            if (expectedVerbs["disallowed"].Any())
                result.Should().NotHaveAllowHeaders(expectedVerbs["disallowed"].ToArray());
        }

        private async Task<TestAbsence> CreateAbsence()
        {
            using var adminScope = fixture.AdminScope();

            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientPostAsync<TestAbsence>(
                $"/persons/{testUser.AzureUniqueId}/absence",
                new CreatePersonAbsenceRequest
                {
                    AppliesFrom = new DateTime(2021, 04, 30),
                    AppliesTo = new DateTime(2022, 04, 30),
                    Comment = "A comment",
                    Type = ApiPersonAbsence.ApiAbsenceType.Absence,
                    AbsencePercentage = 100
                }
            );

            return result.Value;
        }

        private async Task<TestApiInternalRequestModel> CreateChangeRequest(string department)
        {
            var creatorClient = fixture.ApiFactory.CreateClient()
                            .WithTestUser(Users["resourceOwnerCreator"])
                            .AddTestAuthToken();

            using var i = creatorInterceptor = OrgRequestMocker
                 .InterceptOption($"/{testPosition.Id}")
                 .RespondWithHeaders(HttpStatusCode.NoContent, h => h.Add("Allow", "PUT"));

            var req = await creatorClient.CreateDefaultResourceOwnerRequestAsync(
                department, testProject,
                r => r.AsTypeResourceOwner("changeResource"),
                p => p.WithAssignedPerson(fixture.AddProfile(FusionAccountType.Employee))
            );

            await creatorClient.SetChangeParamsAsync(req.Id, DateTime.Today.AddDays(1));
            await creatorClient.ProposePersonAsync(req.Id, fixture.AddProfile(FusionAccountType.Employee));

            return req;
        }

        private async Task<TestApiInternalRequestModel> CreateAndStartRequest()
        {
            var creatorClient = fixture.ApiFactory.CreateClient()
                            .WithTestUser(Users["creator"])
                            .AddTestAuthToken();

            using var i = creatorInterceptor = OrgRequestMocker
                 .InterceptOption($"/{testPosition.Id}")
                 .RespondWithHeaders(HttpStatusCode.NoContent, h => h.Add("Allow", "PUT"));

            return await creatorClient.CreateAndStartDefaultRequestOnPositionAsync(testProject, testPosition);
        }

        private async Task<TestApiInternalRequestModel> CreateRequest()
        {
            var creatorClient = fixture.ApiFactory.CreateClient()
                            .WithTestUser(Users["creator"])
                            .AddTestAuthToken();

            using var i = creatorInterceptor = OrgRequestMocker
                 .InterceptOption($"/{testPosition.Id}")
                 .RespondWithHeaders(HttpStatusCode.NoContent, h => h.Add("Allow", "PUT"));

            return await creatorClient.CreateRequestAsync(testProject.Project.ProjectId,
                req => req.AsTypeNormal().WithPosition(testPosition)
            );
        }


        public Task DisposeAsync()
        {
            creatorInterceptor?.Dispose();
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
    }
}
