using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Api.Tests.FusionMocks;
using Fusion.Resources.Integration.Models.Queue;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
#nullable enable 

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class NormalRequestTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;

        /// <summary>
        ///     Will be generated new for each test
        /// </summary>
        private ApiPersonProfileV3 testUser;


        // Created by the async lifetime
        private TestApiInternalRequestModel normalRequest = null!;
        private FusionTestProjectBuilder testProject = null!;

        private Guid projectId => testProject.Project.ProjectId;

        public NormalRequestTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.External);
        }

        private HttpClient Client => fixture.ApiFactory.CreateClient();

        public async Task InitializeAsync()
        {
            // Mock profile
            testUser = PeopleServiceMock.AddTestProfile()
                .SaveProfile();

            // Mock project
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
            normalRequest = await adminClient.CreateDefaultRequestAsync(testProject, r => r.AsTypeNormal());
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }

        #region Create request tests

        [Fact]
        public async Task CreateRequest_ShouldBeBadRequest_WhenNormalAndNoPosition()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPostAsync($"/projects/{projectId}/requests", new { }, new { Id = Guid.Empty });

            response.Should().BeBadRequest();
        }

        

        [Fact]
        public async Task NormalRequest_Create_ShouldGetNewNumber()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition();
            var response = await Client.TestClientPostAsync($"/projects/{projectId}/requests", new
            {
                type = "normal",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id
            }, new
            {
                number = 0
            });

            response.Should().BeSuccessfull();
            response.Value.number.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task NormalRequest_Create_ShouldBeSuccessfull_WhenProjectDomainIdNull()
        {
            // Mock project
            var newTestProject = new FusionTestProjectBuilder()
                .WithDomainId(null)
                .AddToMockService();

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(newTestProject.Project);

            using var adminScope = fixture.AdminScope();

            var position = newTestProject.AddPosition();
            var response = await Client.TestClientPostAsync($"/projects/{newTestProject.Project.ProjectId}/requests", new
            {
                type = "normal",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id
            }, new { });

            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task NormalRequest_Create_ShouldBeSuccessfull_WhenExitingPosition()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition();
            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests", new
            {
                type = "normal",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id
            });
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task NormalRequest_Create_ShouldHaveIsDraftTrue()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{normalRequest.Id}", new { isDraft = false });
            resp.Should().BeSuccessfull();
            resp.Value.isDraft.Should().BeTrue();
        }

        [Fact]
        public async Task NormalRequest_Create_ShouldHaveWorkflowNull()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{normalRequest.Id}", new { workflow = new { } });
            resp.Should().BeSuccessfull();
            resp.Value.workflow.Should().BeNull();
        }

        [Fact]
        public async Task NormalRequest_Create_ShouldHaveStateNull()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{normalRequest.Id}", new { state = (string?)null });
            resp.Should().BeSuccessfull();
            resp.Value.state.Should().BeNull();
        }

        [Fact]
        public async Task NormalRequest_Create_ShouldBeAbleToSetAssignedDepartmentDirectly()
        {
            using var adminScope = fixture.AdminScope();
            var position = testProject.AddPosition();
            var department = InternalRequestData.RandomDepartment;

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests", new
            {
                type = "normal",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id,
                assignedDepartment = department
            });
            response.Should().BeSuccessfull();
            response.Value.AssignedDepartment.Should().Be(department);
        }
        [Fact]
        public async Task NormalRequest_Create_ShouldNotifyResourceOwner_WhenAssignedDepartmentDirectly()
        {
            using var adminScope = fixture.AdminScope();
            var position = testProject.AddPosition();
            var department = InternalRequestData.RandomDepartment;
            var resourceOwner = LineOrgServiceMock.AddTestUser().MergeWithProfile(testUser).AsResourceOwner().WithFullDepartment(department).SaveProfile();

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests", new
            {
                type = "normal",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id,
                assignedDepartment = department
            });
            response.Should().BeSuccessfull();
            
            NotificationClientMock.SentMessages.Count.Should().BeGreaterThan(0);
            NotificationClientMock.SentMessages.Count(x => x.PersonIdentifier == $"{resourceOwner.AzureUniqueId}").Should().Be(1);
        }

        [Fact]
        public async Task NormalRequest_Create_ShouldBeAbleToProposePersonDirectly()
        {
            using var adminScope = fixture.AdminScope();
            var position = testProject.AddPosition();
            var proposedPerson = fixture.AddProfile(FusionAccountType.Employee);

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests", new
            {
                type = "normal",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id,
                proposedPersonAzureUniqueId = proposedPerson.AzureUniqueId
            });
            response.Should().BeSuccessfull();
            response.Value.ProposedPersonAzureUniqueId.Should().Be(proposedPerson.AzureUniqueId);
            response.Value.ProposedPerson?.Person.Should().NotBeNull();
            response.Value.ProposedPerson?.Person.Mail.Should().Be(proposedPerson.Mail);
        }

        [Fact]
        public async Task NormalRequest_Create_InconcistentDirectAssignment_ShouldGiveBadRequest()
        {
            using var adminScope = fixture.AdminScope();
            var position = testProject.AddPosition();
            var department = InternalRequestData.RandomDepartment;
            var proposedPerson = fixture.AddProfile(FusionAccountType.Employee);
            proposedPerson.FullDepartment = InternalRequestData.PickRandomDepartment(department);

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests", new
            {
                type = "normal",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id,
                assignedDepartment = department,
                proposedPersonAzureUniqueId = proposedPerson.AzureUniqueId
            });
        
            response.Should().BeBadRequest();
        }


        #endregion

        #region Request flow tests

        #region Start
        [Fact]
        public async Task NormalRequest_Start_ShouldBeSuccessfull_WhenStartingRequest()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{normalRequest.Id}/start", null);
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task NormalRequest_Should_Be_Routed_To_Correct_Department()
        {
            var department = "ABC DEF";
            using var adminScope = fixture.AdminScope();

            var matrixRequest = new UpdateResponsibilityMatrixRequest
            {
                ProjectId = testProject.Project.ProjectId,
                LocationId = Guid.NewGuid(),
                Discipline = normalRequest.Discipline,
                BasePositionId = testProject.Positions.First().BasePosition.Id,
                Sector = "ABC",
                Unit = department,
                ResponsibleId = testUser.AzureUniqueId.GetValueOrDefault()
            };

            var matrixResponse = await Client.TestClientPostAsync<TestResponsibilitMatrix>($"/internal-resources/responsibility-matrix", matrixRequest);
            matrixResponse.Should().BeSuccessfull();

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{normalRequest.Id}/start", null);
            response.Should().BeSuccessfull();

            response.Value.AssignedDepartment.Should().Be(department);
        }

        [Theory]
        [InlineData("isDraft", false)]
        [InlineData("state", "created")]
        public async Task NormalRequest_Start_ShouldSet(string property, object value)
        {
            using var adminScope = fixture.AdminScope();

            await Client.StartProjectRequestAsync(testProject, normalRequest.Id);

            var resp = await Client.TestClientGetAsync<JObject>($"/projects/{projectId}/requests/{normalRequest.Id}");
            resp.Should().BeSuccessfull();


            var propertyValue = resp.Value.GetValue(property, StringComparison.OrdinalIgnoreCase);
            var typedValue = propertyValue?.ToObject(value.GetType());

            typedValue.Should().Be(value);
        }

        [Fact]
        public async Task NormalRequest_Start_ShouldAddWorkflowInfo_WhenStartingRequest()
        {
            using var adminScope = fixture.AdminScope();

            await Client.StartProjectRequestAsync(testProject, normalRequest.Id);

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{normalRequest.Id}", new { workflow = new { } });
            resp.Should().BeSuccessfull();
            resp.Value.workflow.Should().NotBeNull();
        }
        #endregion

        #region Proposal state

        [Fact]
        public async Task NormalRequest_Propose_ShouldBeSuccessfull_WhenSettingProposedPerson()
        {
            using var adminScope = fixture.AdminScope();
            var testPerson = fixture.AddProfile(FusionAccountType.Employee);

            var request = await Client.StartProjectRequestAsync(testProject, normalRequest.Id);

            var resp = await Client.TestClientPatchAsync($"/resources/requests/internal/{normalRequest.Id}", new
            {
                proposedPersonAzureUniqueId = testPerson.AzureUniqueId
            }, new
            {
                proposedPersonAzureUniqueId = testPerson.AzureUniqueId,
                proposedPerson = new { person = new { mail = string.Empty, azureUniquePersonId = Guid.Empty } }
            });

            resp.Should().BeSuccessfull();
            resp.Value.proposedPersonAzureUniqueId.Should().Be(testPerson.AzureUniqueId);
            resp.Value.proposedPerson.Should().NotBeNull();
            resp.Value.proposedPerson.person.mail.Should().Be(testPerson.Mail);
            resp.Value.proposedPerson.person.azureUniquePersonId.Should().Be(testPerson.AzureUniqueId!.Value);
        }


        [Fact]
        public async Task NormalRequest_Propose_ShouldBeSuccessfull_WhenApproving()
        {
            using var adminScope = fixture.AdminScope();
            var testPerson = fixture.AddProfile(FusionAccountType.Employee);

            await Client.StartProjectRequestAsync(testProject, normalRequest.Id);
            var assignedRequest = await Client.AssignAnDepartmentAsync(normalRequest.Id);
            await Client.ProposePersonAsync(normalRequest.Id, testPerson);

            var resp = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{assignedRequest.AssignedDepartment}/requests/{normalRequest.Id}/approve", null);

            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task NormalRequest_Propose_ShouldSetState_WhenApproving()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ProposedRequest();

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{normalRequest.Id}", new { state = (string?)null });
            resp.Should().BeSuccessfull();
            resp.Value.state.Should().Be("approval");
        }

        [Fact]
        public async Task NormalRequest_Propose_ShouldSetWorkflow_WhenApproving()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ProposedRequest();

            var resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{normalRequest.Id}");
            resp.Should().BeSuccessfull();

            resp.Value.Workflow.Should().NotBeNull();
            resp.Value.Workflow!.State.Should().Be("Running");

            resp.Value.Workflow.Steps.Should().Contain(s => s.IsCompleted && s.Id == "proposal");
            resp.Value.Workflow.Steps.Should().Contain(s => s.State == "Pending" && s.Id == "approval");
        }

        #endregion

        #region Approval state

        [Fact]
        public async Task NormalRequest_Approval_ShouldBeSuccessfull_WhenApproving()
        {
            using var adminScope = fixture.AdminScope();
            var testPerson = fixture.AddProfile(FusionAccountType.Employee);

            await Client.StartProjectRequestAsync(testProject, normalRequest.Id);
            var assignedRequest = await Client.AssignAnDepartmentAsync(normalRequest.Id);
            await Client.ProposePersonAsync(normalRequest.Id, testPerson);

            await FastForward_ProposedRequest();

            var resp = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{normalRequest.Id}/approve", null);
            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task NormalRequest_Approval_ShouldSetWorkflow_WhenApproving()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ApprovalRequest();

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{normalRequest.Id}", new { workflow = new TestApiWorkflow() });
            resp.Should().BeSuccessfull();

            resp.Value.workflow.Should().NotBeNull();
            resp.Value.workflow.State.Should().Be("Running");

            resp.Value.workflow.Steps.Should().Contain(s => s.IsCompleted && s.Id == "approval");
            resp.Value.workflow.Steps.Should().Contain(s => s.State == "Pending" && s.Id == "provisioning");
        }

        [Fact]
        public async Task NormalRequest_Approval_ShouldQueueProvisioning_WhenApproving()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ApprovalRequest();

            fixture.ApiFactory.queueMock.Verify(q => q.SendMessageAsync(QueuePath.ProvisionPosition, It.Is<ProvisionPositionMessageV1>(q => q.RequestId == normalRequest.Id)), Times.Once);
        }

        [Fact]
        public async Task NormalRequest_Provisioning_ShouldUpdateOrgInstance()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ApprovalRequest();

            var resp = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{normalRequest.Id}/provision", null);
            resp.Should().BeSuccessfull();

            OrgServiceMock.Invocations.Should().Contain(i => i.Method == HttpMethod.Patch && i.Path.Contains($"{normalRequest.OrgPositionInstanceId}"));
        }

        #endregion

        #region Completed state
        [Fact]
        public async Task ProposingChangesShouldGiveBadRequestWhenCompleted()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ApprovalRequest();
            await Client.ProvisionRequestAsync(normalRequest.Id);

            var resp = await Client.TestClientPatchAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{normalRequest.Id}", new
            {
                proposedChanges = new { workload = 50 }
            });
            resp.Should().BeBadRequest();
        }
        #endregion
        #endregion

        #region Query requests

        [Fact]
        public async Task NormalRequest_UsingProjectEndpoint_WhenAllocationAndProposalState_ShouldHideProposals()
        {
            using var adminScope = fixture.AdminScope();
            await StartRequest_WithProposal();

            var resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{normalRequest.Id}");
            resp.Value.ProposedPerson.Should().BeNull();
            resp.Value.ProposedPersonAzureUniqueId.Should().BeNull();
        }
        [Fact]
        public async Task NormalRequest_UsingInternalEndpoint_WhenAllocationAndProposalState_ShouldDisplayProposals()
        {
            using var adminScope = fixture.AdminScope();
            await StartRequest_WithProposal();

            var resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{normalRequest.Id}");

            resp.Value.ProposedPerson.Should().NotBeNull();
            resp.Value.ProposedPersonAzureUniqueId.Should().NotBeNull();
        }

        [Fact]
        public async Task NormalRequests_UsingProjectEndpoint_WhenAllocationAndProposalState_ShouldHideProposals()
        {
            using var adminScope = fixture.AdminScope();
            await StartRequest_WithProposal();

            var respList = await Client.TestClientGetAsync<Testing.Mocks.ApiCollection<TestApiInternalRequestModel>>($"/projects/{projectId}/requests");
            var resp = respList.Value.Value.Single(x => x.Id == normalRequest.Id);
            resp.ProposedPerson.Should().BeNull();
            resp.ProposedPersonAzureUniqueId.Should().BeNull();
        }
        [Fact]
        public async Task NormalRequests_UsingInternalEndpoint_WhenAllocationAndProposalState_ShouldDisplayProposals()
        {
            using var adminScope = fixture.AdminScope();
            await StartRequest_WithProposal();

            var respList = await Client.TestClientGetAsync<Testing.Mocks.ApiCollection<TestApiInternalRequestModel>>($"/resources/requests/internal");
            var resp = respList.Value.Value.Single(x => x.Id == normalRequest.Id);
            resp.ProposedPerson.Should().NotBeNull();
            resp.ProposedPersonAzureUniqueId.Should().NotBeNull();
        }

        #endregion

        private async Task StartRequest_WithProposal()
        {
            var testPerson = fixture.AddProfile(FusionAccountType.Employee);
            await Client.ProposePersonAsync(normalRequest.Id, testPerson);
            await Client.StartProjectRequestAsync(testProject, normalRequest.Id);
        }

        /// <summary>
        /// Perform steps required to end up with a proposed request
        /// </summary>
        /// <returns></returns>
        private async Task FastForward_ProposedRequest()
        {
            var testPerson = fixture.AddProfile(FusionAccountType.Employee);

            await Client.StartProjectRequestAsync(testProject, normalRequest.Id);
            var assignedRequest = await Client.AssignAnDepartmentAsync(normalRequest.Id);
            await Client.ProposePersonAsync(normalRequest.Id, testPerson);

            var resp = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{assignedRequest.AssignedDepartment}/requests/{normalRequest.Id}/approve", null);
            resp.Should().BeSuccessfull();
        }

        private async Task FastForward_ApprovalRequest()
        {
            await FastForward_ProposedRequest();

            var resp = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{normalRequest.Id}/approve", null);
            resp.Should().BeSuccessfull();
        }


    }

}