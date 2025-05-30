﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    [Collection("Integration")]
    public class DirectRequestTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;

        /// <summary>
        ///     Will be generated new for each test
        /// </summary>
        private ApiPersonProfileV3 testUser;


        // Created by the async lifetime
        private TestApiInternalRequestModel directRequest = null!;
        private TestApiInternalRequestModel directRequestWithoutLocation = null!;
        private FusionTestProjectBuilder testProject = null!;

        private Guid projectId => testProject.Project.ProjectId;

        public DirectRequestTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            // Specifying employee user here so to avoid default auto approval
            testUser = PeopleServiceMock.AddTestProfile()
                .WithAccountType(FusionAccountType.Employee)
                .WithFullDepartment("PDP PRD FE TST XN ASD")
                .SaveProfile();
        }

        private HttpClient Client => fixture.ApiFactory.CreateClient();

        public async Task InitializeAsync()
        {
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

            LineOrgServiceMock.AddTestUser().MergeWithProfile(testUser).AsResourceOwner().WithFullDepartment(testUser.FullDepartment).SaveProfile();

            // Create a default request we can work with
            directRequest = await adminClient.CreateDefaultRequestAsync(testProject, r => r
                .AsTypeDirect()
                .WithProposedPerson(testUser)
                .WithAssignedDepartment(testUser.FullDepartment!));

            directRequestWithoutLocation = await adminClient.CreateDefaultRequestAsync(testProject,
                r => r
                    .AsTypeDirect()
                    .WithProposedPerson(testUser)
                    .WithAssignedDepartment(testUser.FullDepartment!),
                p => p
                    .Instances.ForEach(i => i.Location = null));
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }

        #region Create request tests
        [Fact]
        public async Task DirectRequest_Create_ShouldGetNewNumber()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition();
            var response = await Client.TestClientPostAsync($"/projects/{projectId}/requests", new
            {
                type = "normal",
                subType = "direct",
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
        public async Task DirectRequest_Create_ShouldBeSuccessful_WhenProjectDomainIdNull()
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
                subType = "direct",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id
            }, new { });

            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task DirectRequest_Create_ShouldBeSuccessful_WhenExitingPosition()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition();
            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests", new
            {
                type = "normal",
                subType = "direct",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id
            });
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task DirectRequest_Create_ShouldHaveIsDraftTrue()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{directRequest.Id}", new { isDraft = false });
            resp.Should().BeSuccessfull();
            resp.Value.isDraft.Should().BeTrue();
        }

        [Fact]
        public async Task DirectRequest_Create_ShouldHaveWorkflowNull()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{directRequest.Id}", new { workflow = new { } });
            resp.Should().BeSuccessfull();
            resp.Value.workflow.Should().BeNull();
        }

        [Fact]
        public async Task DirectRequest_Create_ShouldHaveStateNull()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{directRequest.Id}", new { state = (string?)null });
            resp.Should().BeSuccessfull();
            resp.Value.state.Should().BeNull();
        }

        [Fact]
        public async Task DirectRequest_Create_ShouldBeAbleToSetAssignedDepartmentDirectly()
        {
            using var adminScope = fixture.AdminScope();
            var position = testProject.AddPosition();
            var department = InternalRequestData.RandomDepartment;

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests", new
            {
                type = "normal",
                subType = "direct",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id,
                assignedDepartment = department
            });
            response.Should().BeSuccessfull();
            response.Value.AssignedDepartment.Should().Be(department);
        }

        [Fact]
        public async Task DirectRequest_Create_ShouldBeAbleToProposePersonDirectly()
        {
            using var adminScope = fixture.AdminScope();
            var position = testProject.AddPosition();
            var proposedPerson = fixture.AddProfile(FusionAccountType.Employee);

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests", new
            {
                type = "normal",
                subType = "direct",
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
        public async Task DirectRequest_Create_ShouldFailWhenProposedPersonNotInAssignedDepartment()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition();
            var proposedPerson = fixture.AddProfile(FusionAccountType.Employee);

            fixture.EnsureDepartment("PDP PRD PCM QRI QRM2");
            fixture.EnsureDepartment("PDP PRD PCM QIA QRM2");


            proposedPerson.FullDepartment = "PDP PRD PCM QRI QRM2";
            proposedPerson.Department = "PCM QRI QRM2";

            var response = await Client.TestClientPostAsync($"/projects/{projectId}/requests", new
            {
                type = "normal",
                subType = "direct",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id,
                proposedPersonAzureUniqueId = proposedPerson.AzureUniqueId,
                assignedDepartment = "PDP PRD PCM QIA QRM2"
            }, new {});

            response.Should().BeBadRequest();
        }


        [Fact]
        public async Task DirectRequest_Create_ShouldBeAllowedWhenProposedPersonIsInSector()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition();
            var proposedPerson = fixture.AddProfile(FusionAccountType.Employee);

            fixture.EnsureDepartment("PDP PRD PCM QRI");
            fixture.EnsureDepartment("PDP PRD PCM QRI QRM2");


            proposedPerson.FullDepartment = "PDP PRD PCM QRI QRM2";
            proposedPerson.Department = "PCM QRI QRM2";

            var response = await Client.TestClientPostAsync($"/projects/{projectId}/requests", new
            {
                type = "normal",
                subType = "direct",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id,
                proposedPersonAzureUniqueId = proposedPerson.AzureUniqueId,
                assignedDepartment = "PDP PRD PCM QRI"
            }, new { assignedDepartment = "" });

            response.Should().BeSuccessfull();
            response.Value.assignedDepartment.Should().Be("PDP PRD PCM QRI");
        }

        #endregion

        #region Request flow tests

        #region Start
        [Fact]
        public async Task DirectRequest_Start_ShouldBeSuccessful_WhenStartingRequest()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{directRequest.Id}/start", null);
            response.Should().BeSuccessfull();
        }

        [Theory]
        [InlineData("isDraft", false)]
        [InlineData("state", "created")]
        public async Task DirectRequest_Start_ShouldSet(string property, object value)
        {
            using var adminScope = fixture.AdminScope();

            await Client.StartProjectRequestAsync(testProject, directRequest.Id);

            var resp = await Client.TestClientGetAsync<JObject>($"/projects/{projectId}/requests/{directRequest.Id}");
            resp.Should().BeSuccessfull();


            var propertyValue = resp.Value.GetValue(property, StringComparison.OrdinalIgnoreCase);
            var typedValue = propertyValue?.ToObject(value.GetType());

            typedValue.Should().Be(value);
        }

        [Fact]
        public async Task DirectRequest_Start_ShouldAssignProposedPersonsDepartment_WhenStarting()
        {
            using var adminScope = fixture.AdminScope();

            var proposedPerson = fixture.AddProfile(FusionAccountType.Employee);
            proposedPerson.FullDepartment = "TST DPT 123";

            var request = await Client.CreateDefaultRequestAsync(testProject,
                r => r.AsTypeDirect().WithProposedPerson(proposedPerson).WithAssignedDepartment(null)
            );

            var result = await Client.StartProjectRequestAsync(testProject, request.Id);
            result.AssignedDepartment.Should().Be(proposedPerson.FullDepartment);
        }

        [Fact]
        public async Task DirectRequest_Start_WhenUserIsExternal()
        {
            using var adminScope = fixture.AdminScope();

            var proposedPerson = fixture.AddProfile(FusionAccountType.External);
            proposedPerson.FullDepartment = null;

            var request = await Client.CreateDefaultRequestAsync(testProject,
                r => r.AsTypeDirect().WithProposedPerson(proposedPerson).WithAssignedDepartment(null)
            );

            var newRequestResponse = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{testProject.Project.ProjectId}/requests/{request.Id}/start", null);
            newRequestResponse.Should().BeSuccessfull();
        }

        [Fact]
        public async Task DirectRequest_Start_ShouldAddWorkflowInfo_WhenStartingRequest()
        {
            using var adminScope = fixture.AdminScope();

            await Client.StartProjectRequestAsync(testProject, directRequest.Id);

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{directRequest.Id}", new { workflow = new { } });
            resp.Should().BeSuccessfull();
            resp.Value.workflow.Should().NotBeNull();
        }
        #endregion

        #region Proposal state

        [Fact]
        public async Task DirectRequest_Propose_ShouldBeSuccessful_WhenApproving()
        {
            using var adminScope = fixture.AdminScope();
            var testPerson = fixture.AddProfile(FusionAccountType.Employee);

            await Client.StartProjectRequestAsync(testProject, directRequest.Id);

            var resp = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{directRequest.AssignedDepartment}/requests/{directRequest.Id}/approve", null);

            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task DirectRequest_Propose_WithChanges_ShouldSetState_WhenApproving()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ProposedRequest(withProposedChanges: new()
            {
                { "workload", 50 }
            });

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{directRequest.Id}", new { state = (string?)null });
            resp.Should().BeSuccessfull();
            resp.Value.state.Should().Be("approval");
        }

        [Fact]
        public async Task DirectRequest_Propose_WithChanges_ShouldSetWorkflow_WhenApproving()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ProposedRequest(withProposedChanges: new()
            {
                { "workload", 99 },
                { "location", new { id = "50ea7407-b417-4dc8-b8f8-2c461e97151b", name = "asia" } }
            });

            var resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{directRequest.Id}");
            resp.Should().BeSuccessfull();

            resp.Value.Workflow.Should().NotBeNull();
            resp.Value.Workflow!.State.Should().Be("Running");

            resp.Value.Workflow.Steps.Should().Contain(s => s.IsCompleted && s.Id == "proposal");
            resp.Value.Workflow.Steps.Should().Contain(s => s.State == "Pending" && s.Id == "approval");
        }

        #endregion

        #region Approval state

        [Fact]
        public async Task DirectRequest_Approval_ShouldBeSuccessful_WhenProposedChanges()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ProposedRequest(withProposedChanges: new()
            {
                { "workload", 50 }
            });

            var resp = await Client.TestClientPostAsync<TestApiInternalRequestModel>(
                $"/projects/{projectId}/requests/{directRequest.Id}/approve", null);
            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task DirectRequest_Approval_ShouldBeSuccessful_WhenProposedNewPerson()
        {
            using var adminScope = fixture.AdminScope();

            var newPerson = fixture.AddProfile(FusionAccountType.Employee);

            await Client.StartProjectRequestAsync(testProject, directRequest.Id);
            var initialPerson = directRequest.ProposedPerson;

            var resp = await Client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/resources/requests/internal/{directRequest.Id}", new
                {
                    ProposedPersonAzureUniqueId = newPerson.AzureUniqueId
                });
            resp.Should().BeSuccessfull();


            resp = await Client.TestClientPostAsync<TestApiInternalRequestModel>(
                $"/projects/{projectId}/requests/{directRequest.Id}/approve", null);
            resp.Should().BeSuccessfull();


            resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>(
                $"/projects/{projectId}/requests/{directRequest.Id}");
            resp.Should().BeSuccessfull();

            resp.Value.Workflow.Should().NotBeNull();
            resp.Value.Workflow.State.Should().Be("Running");

            resp.Value.Workflow.Steps.Should().Contain(s => s.State == "Pending" && s.Id == "approval");
            resp.Value.Workflow.Steps.Should().Contain(s => s.State == "Pending" && s.Id == "provisioning");

            resp.Value.InitialProposedPersonAzureUniqueId.Should().Be(initialPerson!.Person.AzureUniquePersonId);
        }

        [Fact]
        public async Task DirectRequest_Approval_ShouldSetWorkflow_WhenApprovingProposedChanges()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ApprovalRequest(withProposedChanges: new()
            {
                { "workload", 66 }
            });

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{directRequest.Id}", new { workflow = new TestApiWorkflow() });
            resp.Should().BeSuccessfull();

            resp.Value.workflow.Should().NotBeNull();
            resp.Value.workflow.State.Should().Be("Running");

            resp.Value.workflow.Steps.Should().Contain(s => s.IsCompleted && s.Id == "approval");
            resp.Value.workflow.Steps.Should().Contain(s => s.State == "Pending" && s.Id == "provisioning");
        }

        [Fact]
        public async Task DirectRequest_Approval_ShouldQueueProvisioning_WhenApproving()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ApprovalRequest(withProposedChanges: new()
            {
                { "workload", 66 }
            });

            fixture.ApiFactory.queueMock.Verify(q => q.SendMessageDelayedAsync(QueuePath.ProvisionPosition, It.Is<ProvisionPositionMessageV1>(q => q.RequestId == directRequest.Id), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task DirectRequest_Provisioning_ShouldUpdateOrgInstance()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ApprovalRequest(withProposedChanges: new()
            {
                { "workload", 66 }
            });

            var resp = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{directRequest.Id}/provision", null);
            resp.Should().BeSuccessfull();

            OrgServiceMock.Invocations.Should().Contain(i => i.Method == HttpMethod.Patch && i.Path.Contains($"{directRequest.OrgPositionInstanceId}"));
        }

        #endregion

        #region Completed state
        [Fact]
        public async Task ProposingChangesShouldGiveBadRequestWhenCompleted()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ApprovalRequest(withProposedChanges: new()
            {
                { "workload", 66 }
            });

            await Client.ProvisionRequestAsync(directRequest.Id);

            var resp = await Client.TestClientPatchAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{directRequest.Id}", new
            {
                proposedChanges = new { workload = 50 }
            });
            resp.Should().BeBadRequest();
        }
        #endregion

        #region Auto approval
        [Theory]
        [InlineData("Contractor")]
        [InlineData("External")]
        public async Task DirectRequest_AuthApproval_ShouldBeAutoApproved_When(string testCase)
        {
            var position = testProject.AddPosition();

            ApiPersonProfileV3? proposedPerson;

            switch (testCase)
            {
                case "Contractor": proposedPerson = fixture.AddProfile(FusionAccountType.Consultant); break;
                case "External": proposedPerson = fixture.AddProfile(FusionAccountType.External); break;
                default: throw new NotSupportedException("Test case not supported");
            }

            using var adminScope = fixture.AdminScope();
            var testRequest = await Client.CreateDefaultRequestAsync(testProject, r => r
                .AsTypeDirect()
                .WithProposedPerson(proposedPerson));
            
            await Client.StartProjectRequestAsync(testProject, testRequest.Id);

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{testRequest.Id}", new { workflow = new TestApiWorkflow() });
            resp.Should().BeSuccessfull();

            resp.Value.workflow.Should().NotBeNull();
            resp.Value.workflow.State.Should().Be("Running");

            resp.Value.workflow.Steps.Should().Contain(s => s.IsCompleted && s.Id == "approval");
            resp.Value.workflow.Steps.Should().Contain(s => s.State == "Pending" && s.Id == "provisioning");
        }

        [Fact]
        public async Task DirectRequest_AutoApproval_Flow()
        {
            var position = testProject.AddPosition();
            var proposedPerson = fixture.AddProfile(FusionAccountType.Consultant);

            using var adminScope = fixture.AdminScope();
            var testRequest = await Client.CreateDefaultRequestAsync(testProject, r => r
                .AsTypeDirect()
                .WithProposedPerson(proposedPerson));

            // Send the request
            await Client.StartProjectRequestAsync(testProject, testRequest.Id);

            var resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{testRequest.Id}");
            resp.Should().BeSuccessfull();

            #region Validate post start
            resp.Value.Workflow.Should().NotBeNull();

            if (resp.Value.Workflow != null)
            {
                resp.Value.Workflow.State.Should().Be("Running");
                resp.Value.Workflow.Steps.Should().Contain(s => s.Id == "created" && s.IsCompleted && s.State == "Approved");
                resp.Value.Workflow.Steps.Should().Contain(s => s.Id == "proposal" && s.IsCompleted && s.State == "Skipped");
                resp.Value.Workflow.Steps.Should().Contain(s => s.Id == "approval" && s.IsCompleted && s.State == "Skipped");

                resp.Value.Workflow.Steps.Should().Contain(s => s.State == "Pending" && s.Id == "provisioning");
            }
            #endregion

            // Provision
            await Client.ProvisionRequestAsync(testRequest.Id);

            resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{testRequest.Id}");
            resp.Should().BeSuccessfull();

            
            if (resp.Value.Workflow is not null)
            {
                resp.Value.Workflow.Steps.Should().Contain(s => s.State == "Approved" && s.Id == "provisioning");
                resp.Value.Workflow.Steps.Should().OnlyContain(s => s.IsCompleted);
                resp.Value.Workflow.Steps.Should().OnlyContain(s => s.State == "Approved" || s.State == "Skipped");
            }


        }

        [Fact]
        public async Task DirectRequest_AutoApproval_ShouldSkipProposalAndApproval_WhenAutoApprove()
        {
            var position = testProject.AddPosition();
            var proposedPerson = fixture.AddProfile(FusionAccountType.Consultant);

            using var adminScope = fixture.AdminScope();
            var testRequest = await Client.CreateDefaultRequestAsync(testProject, r => r
                .AsTypeDirect()
                .WithProposedPerson(proposedPerson));

            await Client.StartProjectRequestAsync(testProject, testRequest.Id);

            var resp = await Client.TestClientGetAsync($"/projects/{projectId}/requests/{testRequest.Id}", new { workflow = new TestApiWorkflow() });
            resp.Should().BeSuccessfull();

            resp.Value.workflow.Should().NotBeNull();
            resp.Value.workflow.Steps.Should().Contain(s => s.Id == "proposal" && s.IsCompleted && s.State == "Skipped");
            resp.Value.workflow.Steps.Should().Contain(s => s.Id == "approval" && s.IsCompleted && s.State == "Skipped");
        }
        
        [Fact]
        public async Task DirectRequest_AutoApproval_ShouldNotSendNotification_WhenAutoApprove()
        {
            var proposedPerson = PeopleServiceMock
                .AddTestProfile()
                .WithAccountType(FusionAccountType.Consultant)
                .WithFullDepartment("PDP PRD FE TST XN ASD")
                .SaveProfile();

            using var adminScope = fixture.AdminScope();

            var testRequest = await Client.CreateDefaultRequestAsync(testProject, r => r
                .AsTypeDirect()
                .WithAssignedDepartment("PDP PRD FE TST XN ASD")
                .WithProposedPerson(proposedPerson));


            await Client.StartProjectRequestAsync(testProject, testRequest.Id);


            #region assert

            TestLogger.TryLog($"{JsonConvert.SerializeObject(new { testRequest })}");
            TestLogger.TryLog($"{JsonConvert.SerializeObject(NotificationClientMock.SentMessages.ToImmutableList())}");

            var notificationsForRequest = NotificationClientMock.SentMessages.GetNotificationsForRequestId(testRequest.Id);
            notificationsForRequest.Should()
                .BeEmpty($"Should not be any notifications sent for request number {testRequest.Number}");

            #endregion
        }

        [Fact]
        public async Task DirectRequest_ShouldSendNotification_WhenProposedPersonIsEmployee()
        {
            var proposedPerson = PeopleServiceMock
                .AddTestProfile()
                .WithAccountType(FusionAccountType.Employee)
                .WithFullDepartment("PDP PRD FE TST XN ASD")
                .SaveProfile();

            using var adminScope = fixture.AdminScope();

            var testRequest = await Client.CreateDefaultRequestAsync(testProject, r => r
                .AsTypeDirect()
                .WithAssignedDepartment("PDP PRD FE TST XN ASD")
                .WithProposedPerson(proposedPerson));

            await Client.StartProjectRequestAsync(testProject, testRequest.Id);

            var notification = NotificationClientMock.SentMessages.GetNotificationsForRequestId(testRequest.Id);
            notification.Should()
                .HaveCount(1);
        }

        #endregion

        #region Auto accept if no proposed changes

        [Fact]
        public async Task DirectRequest_Propose_WithoutChanges_ShouldAutoAccept()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ProposedRequest();

            var resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>(
                $"/projects/{projectId}/requests/{directRequest.Id}");
            resp.Should().BeSuccessfull();

            resp.Value.Workflow.Should().NotBeNull();
            resp.Value.Workflow!.State.Should().Be("Running");

            resp.Value.Workflow.Steps.Should().Contain(s => s.IsCompleted && s.Id == "approval");
            resp.Value.Workflow.Steps.Should().Contain(s => s.State == "Pending" && s.Id == "provisioning");

            fixture.ApiFactory.queueMock.Verify(
                q => q.SendMessageDelayedAsync(QueuePath.ProvisionPosition,
                    It.Is<ProvisionPositionMessageV1>(q => q.RequestId == directRequest.Id), It.IsAny<int>()),
                Times.Once);
        }

        [Fact]
        public async Task DirectRequest_Propose_WithOnlyLocationChanged_ShouldAutoAccept_WhenLocationWasNull()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ProposedRequest(
                withProposedChanges: new()
                {
                    { "location", new { name = "Stavanger" } },
                },
                request: directRequestWithoutLocation);

            var resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>(
                $"/projects/{projectId}/requests/{directRequestWithoutLocation.Id}");
            resp.Should().BeSuccessfull();

            resp.Value.Workflow.Should().NotBeNull();
            resp.Value.Workflow!.State.Should().Be("Running");

            resp.Value.Workflow.Steps.Should().Contain(s => s.IsCompleted && s.Id == "approval");
            resp.Value.Workflow.Steps.Should().Contain(s => s.State == "Pending" && s.Id == "provisioning");

            fixture.ApiFactory.queueMock.Verify(
                q => q.SendMessageDelayedAsync(QueuePath.ProvisionPosition,
                    It.Is<ProvisionPositionMessageV1>(q => q.RequestId == directRequestWithoutLocation.Id), It.IsAny<int>()),
                    Times.Once);
        }

        [Fact]
        public async Task DirectRequest_Propose_WithOnlyLocationChanged_ShouldRequireApproval_WhenLocationWasSet()
        {
            using var adminScope = fixture.AdminScope();

            await FastForward_ProposedRequest(withProposedChanges: new()
            {
                { "location", new { name = "Stavanger" } },
            });

            var resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>(
                $"/projects/{projectId}/requests/{directRequest.Id}");
            resp.Should().BeSuccessfull();

            resp.Value.Workflow.Should().NotBeNull();
            resp.Value.Workflow!.State.Should().Be("Running");

            resp.Value.Workflow.Steps.Should().Contain(s => s.IsCompleted && s.Id == "proposal");
            resp.Value.Workflow.Steps.Should().Contain(s => s.State == "Pending" && s.Id == "approval");
        }

        #endregion
        
        
        #endregion

        #region Query requests

        [Fact]
        public async Task DirectRequest_UsingProjectEndpoint_WhenAllocationAndProposalState_ShouldDisplayProposals()
        {
            using var adminScope = fixture.AdminScope();
            await Client.StartProjectRequestAsync(testProject, directRequest.Id);

            var resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{directRequest.Id}");
            resp.Value.ProposedPerson.Should().NotBeNull();
            resp.Value.ProposedPersonAzureUniqueId.Should().NotBeNull();
        }
        [Fact]
        public async Task DirectRequest_UsingInternalEndpoint_WhenAllocationAndProposalState_ShouldDisplayProposals()
        {
            using var adminScope = fixture.AdminScope();
            await Client.StartProjectRequestAsync(testProject, directRequest.Id);

            var resp = await Client.TestClientGetAsync<TestApiInternalRequestModel>($"/resources/requests/internal/{directRequest.Id}");

            resp.Value.ProposedPerson.Should().NotBeNull();
            resp.Value.ProposedPersonAzureUniqueId.Should().NotBeNull();
        }

        [Fact]
        public async Task DirectRequests_UsingProjectEndpoint_WhenAllocationAndProposalState_ShouldDisplayProposals()
        {
            using var adminScope = fixture.AdminScope();
            await Client.StartProjectRequestAsync(testProject, directRequest.Id);

            var respList =
                await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(
                    $"/projects/{projectId}/requests");
            var resp = respList.Value.Value.Single(x => x.Id == directRequest.Id);
            resp.ProposedPerson.Should().NotBeNull();
            resp.ProposedPersonAzureUniqueId.Should().NotBeNull();
        }
        [Fact]
        public async Task DirectRequests_UsingInternalEndpoint_WhenAllocationAndProposalState_ShouldDisplayProposals()
        {
            using var adminScope = fixture.AdminScope();
            await Client.StartProjectRequestAsync(testProject, directRequest.Id);

            var respList =
                await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(
                    $"/resources/requests/internal");
            var resp = respList.Value.Value.Single(x => x.Id == directRequest.Id);
            resp.ProposedPerson.Should().NotBeNull();
            resp.ProposedPersonAzureUniqueId.Should().NotBeNull();
        }

        #endregion

        /// <summary>
        /// Perform steps required to end up with a proposed request
        /// </summary>
        /// <returns></returns>
        private async Task FastForward_ProposedRequest(Dictionary<string, object>? withProposedChanges = null, TestApiInternalRequestModel? request = null)
        {
            request ??= directRequest;

            if (withProposedChanges != null)
            {
                await Client.TestClientPatchAsync<TestApiInternalRequestModel>(
                    $"/resources/requests/internal/{request.Id}", new
                    {
                        proposedChanges = withProposedChanges
                    });
            }

            await Client.StartProjectRequestAsync(testProject, request.Id);

            var resp = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{request.AssignedDepartment}/requests/{request.Id}/approve", null);
            resp.Should().BeSuccessfull();
        }

        private async Task FastForward_ApprovalRequest(Dictionary<string, object>? withProposedChanges = null)
        {
            await FastForward_ProposedRequest(withProposedChanges: withProposedChanges);

            var resp = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{directRequest.Id}/approve", null);
            resp.Should().BeSuccessfull();
        }
    }
}