using Azure.Core;
using FluentAssertions;
using Fusion.ApiClients.Org;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Api.Tests.FusionMocks;
using Fusion.Resources.Api.Tests.IntegrationTests;
using Fusion.Resources.Domain;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using Moq;
using System;
using System.Collections.Generic;
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

        const string ExactScope = "TPD PRD TST ASD";
        const string WildcardScope = "TPD PRD TST *";
        const string UnrelatedScope = "EPN SUB AXS";
        const string ParentScope = "TPD PRD TST";
        const string SiblingScope = "TPD PRD TST FGH";

        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private FusionTestProjectBuilder testProject;

        private ApiPersonProfileV3 testUser;

        private ApiPositionV2 testPosition;
        private ApiPositionV2 taskOwnerPosition;

        private OrgRequestInterceptor creatorInterceptor;

        public enum ManagerRoleType { None, ResourceOwner, DelegatedResourceOwner }


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
            SetupManagerRole(ManagerRoleType.ResourceOwner, resourceOwnerCreator, TestDepartment);

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
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, false)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, false)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, false)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, false)]
        public async Task CanDeleteRequestAssignedToDepartment(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();

            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientDeleteAsync<dynamic>($"/projects/{testProject.Project.ProjectId}/requests/{request.Id}");

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, false)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, false)]
        [InlineData(ManagerRoleType.None, TestDepartment, false)]
        public async Task CanDeleteInvalidRequestAssignedToDepartment(ManagerRoleType role, string department, bool shouldBeAllowed, bool removeInstanceInstead = false)
        {
            var request = await CreateAndStartRequest();

            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            InvalidateRequest(request, removeInstance: removeInstanceInstead);

            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientOptionsAsync($"/projects/{testProject.Project.ProjectId}/requests/{request.Id}");

            if (shouldBeAllowed) result.Should().HaveAllowHeaders(HttpMethod.Delete);
            else result.Should().NotHaveAllowHeaders(HttpMethod.Delete);

            result = await client.TestClientDeleteAsync<dynamic>($"/projects/{testProject.Project.ProjectId}/requests/{request.Id}");

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanReadRequestsAssignedToDepartment(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();

            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientGetAsync<TestApiInternalRequestModel>($"/departments/{request.AssignedDepartment}/resources/requests/{request.Id}");

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanEditGeneralOnRequestAssignedToDepartment(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();

            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

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
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, false)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, false)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, false)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, false)]
        public async Task CanEditAdditionalCommentOnRequestAssignedToDepartment(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

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
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanReassignDepartmentOnRequest(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            const string changedDepartment = "TPD UPD ASD";
            fixture.EnsureDepartment(changedDepartment);
            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);


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

            using (var userScope = fixture.UserScope(actor))
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
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, true)]
        [InlineData(ManagerRoleType.ResourceOwner, "PDP PRD FE ANE ANE5", true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanAssignDepartmentOnUnassignedRequest(ManagerRoleType role, string department, bool shouldBeAllowed)
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
            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/resources/requests/internal/{request.Id}",
                new { assignedDepartment = changedDepartment }
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanCreateResourceOwnerRequest(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

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
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, false)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, false)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, false)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, false)]
        public async Task CanStartNormalRequest(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var request = await CreateRequest();
            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPostAsync<dynamic>(
                $"/projects/{testProject.Project.ProjectId}/requests/{request.Id}/start", null
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        //[InlineData("creator", "TPD RND WQE FQE", false)]
        public async Task CanProposePersonNormalRequest(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            var proposedPerson = PeopleServiceMock.AddTestProfile()
                .SaveProfile();

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/resources/requests/internal/{request.Id}",
                new { proposedPersonAzureUniqueId = proposedPerson.AzureUniqueId });

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }
        public enum ActorType { Manager, TaskOwner, RequestCreator }
        [Theory]
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, SameL2Department, true)]
        [InlineData(ActorType.TaskOwner, ManagerRoleType.None, TestDepartment, false)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanProposeNormalRequest(ActorType actorType, ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            var actor = actorType switch
            {
                ActorType.Manager => fixture.AddProfile(FusionAccountType.Employee),
                ActorType.TaskOwner => Users["taskOwner"],
                _ => throw new NotSupportedException("Unsupported actor type")
            };
            SetupManagerRole(role, actor, department);

            using (var adminScope = fixture.AdminScope())
            {
                var proposedPerson = PeopleServiceMock.AddTestProfile()
                    .SaveProfile();

                var adminClient = fixture.ApiFactory.CreateClient();

                await adminClient.AssignDepartmentAsync(request.Id, TestDepartment);
                await adminClient.ProposePersonAsync(request.Id, proposedPerson);
            }

            using var userScope = fixture.UserScope(actor);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{TestDepartment}/resources/requests/{request.Id}/approve", null);

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, TestDepartment, false)]
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, SiblingDepartment, false)]
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, ParentDepartment, false)]
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, SameL2Department, false)]
        [InlineData(ActorType.TaskOwner, ManagerRoleType.None, TestDepartment, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, ExactScope, false)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, WildcardScope, false)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, ParentScope, false)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, SiblingScope, false)]
        public async Task CanAcceptNormalRequest(ActorType actorType, ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            var actor = actorType switch
            {
                ActorType.Manager => fixture.AddProfile(FusionAccountType.Employee),
                ActorType.TaskOwner => Users["taskOwner"],
                _ => throw new NotSupportedException("Unsupported actor type")
            };
            SetupManagerRole(role, actor, department);

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

            using var userScope = fixture.UserScope(actor);
            if (actorType == ActorType.TaskOwner)
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
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.ResourceOwner, SameL2Department, true)]
        // This should be reconsidered. Just being the creator should not give you any additional access, as roles can be changed.
        [InlineData(ActorType.RequestCreator, ManagerRoleType.None, TestDepartment, true)]
        [InlineData(ActorType.TaskOwner, ManagerRoleType.None, TestDepartment, false)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ActorType.Manager, ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanStartChangeRequest(ActorType actorType, ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var request = await CreateChangeRequest(TestDepartment);

            var client = fixture.ApiFactory.CreateClient();
            using (var adminscope = fixture.AdminScope())
            {
                var testUser = fixture.AddProfile(FusionAccountType.Employee);

                await client.SetChangeParamsAsync(request.Id, DateTime.Today.AddDays(1));
                await client.ProposePersonAsync(request.Id, testUser);
            }

            var actor = actorType switch
            {
                ActorType.Manager => fixture.AddProfile(FusionAccountType.Employee),
                ActorType.TaskOwner => Users["taskOwner"],
                ActorType.RequestCreator => Users["resourceOwnerCreator"],
                _ => throw new NotSupportedException("Unsupported actor type")
            };
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            var result = await client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{request.AssignedDepartment}/resources/requests/{request.Id}/start", null);

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanAddPersonAbsence(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var client = fixture.ApiFactory.CreateClient();

            // Setup the subject we want to create absence on
            var testSubject = fixture.AddProfile(FusionAccountType.Employee);
            testSubject.FullDepartment = TestDepartment;


            // Create the actor we want to confirm permissions for
            var testUser = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, testUser, department);

            using var userScope = fixture.UserScope(testUser);

            var result = await client.TestClientPostAsync<TestAbsence>(
                $"/persons/{testSubject.AzureUniqueId}/absence",
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
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanEditPersonAbsence(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var client = fixture.ApiFactory.CreateClient();
            var absence = await CreateAbsence();

            // Create the actor we want to confirm permissions for
            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);


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
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanDeletePersonAbsence(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var absence = await CreateAbsence();

            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientDeleteAsync<dynamic>(
                $"/persons/{testUser.AzureUniqueId}/absence/{absence.Id}"
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanGetPersonAbsence(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            var absence = await CreateAbsence();

            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientGetAsync<TestAbsence>(
                $"/persons/{testUser.AzureUniqueId}/absence/{absence.Id}"
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        public enum AbsenceAccessLevel { None, All, Limited, OtherTasksOnly }
        [Theory]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.ResourceOwner, TestDepartment, AbsenceAccessLevel.All)]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.ResourceOwner, SiblingDepartment, AbsenceAccessLevel.All)]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.ResourceOwner, ParentDepartment, AbsenceAccessLevel.All)]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.ResourceOwner, SameL2Department, AbsenceAccessLevel.Limited)]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.DelegatedResourceOwner, ExactScope, AbsenceAccessLevel.All)]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.DelegatedResourceOwner, WildcardScope, AbsenceAccessLevel.All)]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, AbsenceAccessLevel.OtherTasksOnly)]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.None, UnrelatedScope, AbsenceAccessLevel.OtherTasksOnly)]
        [InlineData(FusionAccountType.Consultant, ManagerRoleType.None, UnrelatedScope, AbsenceAccessLevel.None)]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.DelegatedResourceOwner, ParentScope, AbsenceAccessLevel.All)]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.DelegatedResourceOwner, SiblingScope, AbsenceAccessLevel.All)]
        public async Task CanGetAllAbsenceForPerson(FusionAccountType accountType, ManagerRoleType role, string department, AbsenceAccessLevel accessLevel)
        {
            var absence = await CreateAbsence();
            var privateAdditionlTask = await CreateAdditionlTask(true);
            var additionlTask = await CreateAdditionlTask(false);


            var actor = fixture.AddProfile(accountType);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientGetAsync<TestAbsenceCollection>($"/persons/{testUser.AzureUniqueId}/absence");


            switch (accessLevel)
            {
                case AbsenceAccessLevel.All:
                case AbsenceAccessLevel.Limited:    // Limited auth should contain all elements, but hide details if marked private.
                    result.Should().BeSuccessfull();
                    result.Value.Value.Should().Contain(a => a.Id == absence.Id, "should contain absence");
                    result.Value.Value.Should().Contain(a => a.Id == additionlTask.Id, "should contain other task");
                    result.Value.Value.Should().Contain(a => a.Id == privateAdditionlTask.Id, "should contain private other task");
                    break;

                case AbsenceAccessLevel.OtherTasksOnly:
                    result.Should().BeSuccessfull();
                    result.Value.Value.Should().NotContain(a => a.Id == absence.Id, "absence should not be displayed in limited mode");
                    result.Value.Value.Should().NotContain(a => a.Id == privateAdditionlTask.Id, "should not contain private other task");
                    result.Value.Value.Should().Contain(a => a.Id == additionlTask.Id, "should contain other task");
                    break;

                case AbsenceAccessLevel.None:
                    result.Should().BeUnauthorized();
                    break;


            }
            //if (shouldBeAllowed) result.Should().BeSuccessfull();
            //else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.ResourceOwner, TestDepartment, "GET,POST")]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.ResourceOwner, SiblingDepartment, "GET,POST")]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.ResourceOwner, ParentDepartment, "GET,POST")]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.ResourceOwner, SameL2Department, "GET,!POST")]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.DelegatedResourceOwner, ExactScope, "GET,POST")]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.DelegatedResourceOwner, WildcardScope, "GET,POST")]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, "GET,!POST")]
        [InlineData(FusionAccountType.Consultant, ManagerRoleType.None, UnrelatedScope, "!GET,!POST")]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.DelegatedResourceOwner, ParentScope, "GET,POST")]
        [InlineData(FusionAccountType.Employee, ManagerRoleType.DelegatedResourceOwner, SiblingScope, "GET,POST")]
        public async Task CanGetAbsenceOptionsForPerson(FusionAccountType accountType, ManagerRoleType role, string department, string allowed)
        {
            var actor = fixture.AddProfile(accountType);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientOptionsAsync($"/persons/{testUser.AzureUniqueId}/absence");

            result.CheckAllowHeader(allowed);
        }

        [Theory]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, "GET,PUT,DELETE")]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, "GET,PUT,DELETE")]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, "GET,PUT,DELETE")]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, "GET,!PUT,!DELETE")]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, "GET,PUT,DELETE")]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, "GET,PUT,DELETE")]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, "GET,PUT,DELETE")]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, "GET,PUT,DELETE")]
        public async Task CanGetAbsenceOptions(ManagerRoleType role, string department, string allowed)
        {
            var absence = await CreateAbsence();

            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);

            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientOptionsAsync($"/persons/{testUser.AzureUniqueId}/absence/{absence.Id}");

            result.CheckAllowHeader(allowed);
        }

        [Theory]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanGetDepartmentUnassignedRequests(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            fixture.EnsureDepartment(TestDepartment);

            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientGetAsync<dynamic>($"/departments/{TestDepartment}/resources/requests/unassigned");

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, "GET,PATCH")]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, "GET,PATCH")]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, "GET,PATCH")]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, "GET,PATCH")]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, "GET,PATCH")]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, "GET,PATCH")]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, "GET,PATCH")]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, "GET,PATCH")]
        public async Task CanGetOptionsDepartmentUnassignedRequests(ManagerRoleType role, string department, string allowedVerbs)
        {
            fixture.EnsureDepartment(TestDepartment);


            var request = await CreateChangeRequest(TestDepartment);

            using (var adminscope = fixture.AdminScope())
            {
                var client = fixture.ApiFactory.CreateClient();
                var testUser = fixture.AddProfile(FusionAccountType.Employee);

                await client.AssignDepartmentAsync(request.Id, null);
            }

            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using (var userScope = fixture.UserScope(actor))
            {
                var client = fixture.ApiFactory.CreateClient();

                var result = await client.TestClientOptionsAsync(
                    $"/projects/{request.Project.Id}/requests/{request.Id}"
                );
                result.CheckAllowHeader(allowedVerbs);
            }
        }

        [Theory]
        [InlineData(ManagerRoleType.ResourceOwner, TestDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SiblingDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, ParentDepartment, true)]
        [InlineData(ManagerRoleType.ResourceOwner, SameL2Department, true)]
        [InlineData(ManagerRoleType.ResourceOwner, "PDP PRS XXX YYY", false)]
        [InlineData(ManagerRoleType.ResourceOwner, "CFO GBS XXX YYY", false)]
        [InlineData(ManagerRoleType.ResourceOwner, "TDI XXX YYY", false)]
        [InlineData(ManagerRoleType.ResourceOwner, "CFO SBG YYY", false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ExactScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, WildcardScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, UnrelatedScope, false)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, ParentScope, true)]
        [InlineData(ManagerRoleType.DelegatedResourceOwner, SiblingScope, true)]
        public async Task CanGetInternalRequests(ManagerRoleType role, string department, bool shouldBeAllowed)
        {
            fixture.EnsureDepartment(TestDepartment);

            var actor = fixture.AddProfile(FusionAccountType.Employee);
            SetupManagerRole(role, actor, department);

            using var userScope = fixture.UserScope(actor);
            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientGetAsync<dynamic>($"/resources/requests/internal?$filter=assignedDepartment eq {TestDepartment}");

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }


        /// <summary>
        /// Create an absence which is active, today +- 10 days.
        /// </summary>
        /// <returns></returns>
        private async Task<TestAbsence> CreateAbsence()
        {
            using var adminScope = fixture.AdminScope();

            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientPostAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence", new
            {
                appliesFrom = DateTime.Today.AddDays(-10),
                appliesTo = DateTime.Today.AddDays(10),
                comment = "A comment",
                type = "absence",
                absencePercentage = 100
            });

            return result.Value;
        }
        /// <summary>
        /// Create absence of type otherTasks, which is active, today +- 10 days.
        /// </summary>
        /// <param name="isPrivate">Should be marked private</param>
        /// <returns></returns>
        private async Task<TestAbsence> CreateAdditionlTask(bool isPrivate)
        {
            using var adminScope = fixture.AdminScope();

            var client = fixture.ApiFactory.CreateClient();

            var result = await client.TestClientPostAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence", new
            {
                appliesFrom = DateTime.Today.AddDays(-10),
                appliesTo = DateTime.Today.AddDays(10),
                comment = "A comment",
                type = "otherTasks",
                absencePercentage = 100,
                isPrivate = isPrivate,
                TaskDetails = new TestTaskDetails() { Location = "Top secret location", RoleName = "Top secret role name" },
            });

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

        /// <summary>
        /// Will set up the provided test user as manager as configured in "SAP", or with a local delegated resource owner.
        /// </summary>
        private void SetupManagerRole(ManagerRoleType role, ApiPersonProfileV3 testUser, string fullDepartment)
        {
            switch (role)
            {
                case ManagerRoleType.ResourceOwner:
                    fixture.SetAsResourceOwner(testUser, fullDepartment);
                    break;
                case ManagerRoleType.DelegatedResourceOwner:
                    testUser.WithDelegatedManagerRole(fullDepartment);
                    break;
                case ManagerRoleType.None:
                    break;
                default:
                    throw new NotSupportedException("Role setup not supported");

            }
        }


        private Task<TestApiInternalRequestModel> CreateAndStartRequest()
            => CreateAndStartRequest(testPosition);

        private void InvalidateRequest(TestApiInternalRequestModel request, bool removeInstance)
        {
            var id = (removeInstance ? request.OrgPositionInstanceId : request.OrgPositionId)!.Value;

            if (removeInstance)
                OrgServiceMock.RemoveInstance(id);
            else
                OrgServiceMock.RemovePosition(id);
        }
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
