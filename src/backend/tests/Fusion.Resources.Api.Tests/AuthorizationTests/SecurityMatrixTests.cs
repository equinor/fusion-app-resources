using Fusion.ApiClients.Org;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        
        private ApiPersonProfileV3 testUser;
        
        private ApiPositionV2 testPosition;
        private ApiPositionV2 taskOwnerPosition;

        private OrgRequestInterceptor creatorInterceptor;

        public Dictionary<string, ApiPersonProfileV3> Users { get; private set; }
        
        public SecurityMatrixTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            fixture.DisableMemoryCache();

            fixture.EnsureDepartment(TestDepartment);

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            testProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .WithProperty("pimsWriteSyncEnabled", true)
                .AddToMockService();

            var creator = fixture.AddProfile(FusionAccountType.Employee);
            var resourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            resourceOwner.IsResourceOwner = true;

            var resourceOwnerCreator = fixture.AddProfile(FusionAccountType.Employee);
            resourceOwnerCreator.IsResourceOwner = true;
            resourceOwnerCreator.FullDepartment = TestDepartment;

            var taskOwner = fixture.AddProfile(FusionAccountType.Employee);
            //var taskOwnerBasePosition = testProject.AddBasePosition($"TO: {Guid.NewGuid()}");
            taskOwnerPosition = testProject.AddPosition()
                .WithAssignedPerson(taskOwner);

            fixture.ContextResolver
               .AddContext(testProject.Project);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = TestDepartment);
            testPosition = testProject.AddPosition()
                .WithBasePosition(bp)
                .WithAssignedPerson(fixture.AddProfile(FusionAccountType.Employee))
                .WithEnsuredFutureInstances()
                .WithTaskOwner(taskOwnerPosition.Id);

            OrgServiceMock.SetTaskOwner(testPosition.Id, taskOwnerPosition.Id);

            Users = new Dictionary<string, ApiPersonProfileV3>()
            {
                ["creator"] = creator,
                ["resourceOwner"] = resourceOwner,
                ["resourceOwnerCreator"] = resourceOwnerCreator,
                ["taskOwner"] = taskOwner
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
        public async Task CanReadRequestsAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientGetAsync<TestApiInternalRequestModel>($"/departments/{request.AssignedDepartment}/resources/requests/{request.Id}");

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, true)]
        public async Task CanEditGeneralOnRequestAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/departments/{request.AssignedDepartment}/resources/requests/{request.Id}",
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
        public async Task CanReassignDepartmentOnRequest(string role, string department, bool shouldBeAllowed)
        {
            const string changedDepartment = "TPD UPD ASD";
            fixture.EnsureDepartment(changedDepartment);
            Users[role].FullDepartment = department;

            var request = await CreateAndStartRequest();
            using (var adminScope = fixture.AdminScope())
            {
                var client = fixture.ApiFactory.CreateClient();
                var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                    $"/departments/{request.AssignedDepartment}/resources/requests/{request.Id}",
                    new { assignedDepartment = TestDepartment }
                );
                result.Should().BeSuccessfull();
            }

            using (var userScope = fixture.UserScope(Users[role]))
            {
                var client = fixture.ApiFactory.CreateClient();
                var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                    $"/departments/{request.AssignedDepartment}/resources/requests/{request.Id}",
                    new { assignedDepartment = changedDepartment }
                );

                if (shouldBeAllowed) result.Should().BeSuccessfull();
                else result.Should().BeUnauthorized();
            }
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("resourceOwner", "PDP PRD FE ANE ANE5", true)]
        public async Task CanAssignDepartmentOnUnassignedRequest(string role, string department, bool shouldBeAllowed)
        {
            const string changedDepartment = "TDI UPD QWE RTY1";
            fixture.EnsureDepartment(changedDepartment);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = "TDI UPD QWE RTY");
            var position = testProject.AddPosition()
                .WithBasePosition(bp)
                .WithAssignedPerson(fixture.AddProfile(FusionAccountType.Employee))
                .WithEnsuredFutureInstances()
                .WithTaskOwner(taskOwnerPosition.Id);

            var request = await CreateAndStartRequest(position);
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/resources/requests/internal/{request.Id}",
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

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = TestDepartment);
            var taskOwner = fixture.AddProfile(FusionAccountType.Employee);
            var taskOwnerPosition = testProject.AddPosition()
                .WithAssignedPerson(taskOwner);
            
            var assignedPerson = PeopleServiceMock.AddTestProfile().WithAccountType(FusionAccountType.Employee).WithFullDepartment(TestDepartment).WithDepartment(department).SaveProfile();

            testPosition = testProject.AddPosition()
                .WithBasePosition(bp)
                .WithAssignedPerson(assignedPerson)
                .WithEnsuredFutureInstances()
                .WithTaskOwner(taskOwnerPosition.Id);

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
        //[InlineData("creator", "TPD RND WQE FQE", false)]
        public async Task CanProposePersonNormalRequest(string role, string department, bool shouldBeAllowed)
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
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("taskOwner", TestDepartment, false)]

        public async Task CanProposeNormalRequest(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;

            using (var adminScope = fixture.AdminScope())
            {
                var proposedPerson = PeopleServiceMock.AddTestProfile()
                    .SaveProfile();

                var adminClient = fixture.ApiFactory.CreateClient();

                await adminClient.AssignDepartmentAsync(request.Id, TestDepartment);
                await adminClient.ProposePersonAsync(request.Id, proposedPerson);
            }

            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPostAsync<TestApiInternalRequestModel>(
                $"/departments/{TestDepartment}/resources/requests/{request.Id}/approve",
                null
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, false)]
        [InlineData("resourceOwner", SiblingDepartment, false)]
        [InlineData("resourceOwner", ParentDepartment, false)]
        [InlineData("resourceOwner", SameL2Department, false)]
        [InlineData("taskOwner", TestDepartment, true)]
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
                await adminClient.TestClientPostAsync<TestApiInternalRequestModel>(
                    $"/projects/{testProject.Project.ProjectId}/resources/requests/{request.Id}/approve",
                    null
                );
            }

            OrgRequestInterceptor taskOwnerInterceptor = null;

            using var userScope = fixture.UserScope(Users[role]);
            if(role == "taskOwner")
            {
                taskOwnerInterceptor = OrgRequestMocker
                    .InterceptOption($"/{testPosition.Id}")
                    .RespondWithHeaders(HttpStatusCode.NoContent, h => h.Add("Allow", "PUT"));
            }

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPostAsync<TestApiInternalRequestModel>(
               $"/projects/{testProject.Project.ProjectId}/resources/requests/{request.Id}/approve",
               null
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();

            taskOwnerInterceptor?.Dispose();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("resourceOwnerCreator", TestDepartment, true)]
        [InlineData("taskOwner", TestDepartment, false)]
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
        [InlineData("resourceOwner", SameL2Department, true)]
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
        [InlineData("resourceOwner", SameL2Department, true)]
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
        [InlineData("resourceOwner", SameL2Department, "GET,!POST")]
        public async Task CanGetAbsenceOptionsForPerson(string role, string department, string allowed)
        {
            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientOptionsAsync(
                $"/persons/{testUser.AzureUniqueId}/absence"
            );

            CheckAllowHeader(allowed, result);
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, "GET,PUT,DELETE")]
        [InlineData("resourceOwner", SiblingDepartment, "GET,PUT,DELETE")]
        [InlineData("resourceOwner", ParentDepartment, "GET,PUT,DELETE")]
        [InlineData("resourceOwner", SameL2Department, "GET,!PUT,!DELETE")]
        public async Task CanGetAbsenceOptions(string role, string department, string allowed)
        {
            var absence = await CreateAbsence();

            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientOptionsAsync(
                $"/persons/{testUser.AzureUniqueId}/absence/{absence.Id}"
            );

            CheckAllowHeader(allowed, result);
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, true)]
        public async Task CanGetDepartmentUnassignedRequests(string role, string department, bool shouldBeAllowed)
        {
            fixture.EnsureDepartment(TestDepartment);
            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientGetAsync<dynamic>(
                $"/departments/{TestDepartment}/resources/requests/unassigned"
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, "GET,PATCH")]
        [InlineData("resourceOwner", SiblingDepartment, "GET,PATCH")]
        [InlineData("resourceOwner", ParentDepartment, "GET,PATCH")]
        [InlineData("resourceOwner", SameL2Department, "GET,PATCH")]
        public async Task CanGetOptionsDepartmentUnassignedRequests(string role, string department, string allowedVerbs)
        {
            var request = await CreateChangeRequest(TestDepartment);

            using (var adminscope = fixture.AdminScope())
            {
                var client = fixture.ApiFactory.CreateClient();
                var testUser = fixture.AddProfile(FusionAccountType.Employee);

                await client.AssignDepartmentAsync(request.Id, null);
            }

            fixture.EnsureDepartment(TestDepartment);
            using (var userScope = fixture.UserScope(Users[role]))
            {
                Users[role].FullDepartment = department;
                var client = fixture.ApiFactory.CreateClient();

                var result = await client.TestClientOptionsAsync(
                    $"/projects/{request.Project.Id}/requests/{request.Id}"
                );
                CheckAllowHeader(allowedVerbs, result);
            }
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        [InlineData("resourceOwner", ParentDepartment, true)]
        [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("resourceOwner", "PDP PRS XXX YYY", true)]
        [InlineData("resourceOwner", "CFO GBS XXX YYY", true)]
        [InlineData("resourceOwner", "TDI XXX YYY", true)]
        [InlineData("resourceOwner", "CFO SBG YYY", true)]
        public async Task CanGetInternalRequests(string role, string department, bool shouldBeAllowed)
        {
            fixture.EnsureDepartment(TestDepartment);
            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientGetAsync<dynamic>($"/resources/requests/internal?$filter=assignedDepartment eq {department}");

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        private static void CheckAllowHeader(string allowed, TestClientHttpResponse<dynamic> result)
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

            var assignedPerson = PeopleServiceMock.AddTestProfile().WithAccountType(FusionAccountType.Employee).WithFullDepartment(department).WithDepartment(department).SaveProfile();

            var req = await creatorClient.CreateDefaultResourceOwnerRequestAsync(
                department, testProject,
                r => r.AsTypeResourceOwner("changeResource"),
                p => p.WithAssignedPerson(assignedPerson)
            );

            await creatorClient.SetChangeParamsAsync(req.Id, DateTime.Today.AddDays(1));
            await creatorClient.ProposePersonAsync(req.Id, fixture.AddProfile(FusionAccountType.Employee));

            return req;
        }

        private Task<TestApiInternalRequestModel> CreateAndStartRequest()
            => CreateAndStartRequest(testPosition);
        private async Task<TestApiInternalRequestModel> CreateAndStartRequest(ApiPositionV2 position)
        {
            var creatorClient = fixture.ApiFactory.CreateClient()
                            .WithTestUser(Users["creator"])
                            .AddTestAuthToken();

            using var i = creatorInterceptor = OrgRequestMocker
                 .InterceptOption($"/{position.Id}")
                 .RespondWithHeaders(HttpStatusCode.NoContent, h => h.Add("Allow", "PUT"));

            return await creatorClient.CreateAndStartDefaultRequestOnPositionAsync(testProject, position);
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
