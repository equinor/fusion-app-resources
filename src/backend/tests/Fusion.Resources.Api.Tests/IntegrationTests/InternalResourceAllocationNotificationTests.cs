using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Fusion.ApiClients.Org;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Api.Tests.FusionMocks;
using Fusion.Testing;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using Xunit;
using Xunit.Abstractions;
#nullable enable 

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class InternalResourceAllocationNotificationTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private const string TestDepartmentId = "TPD PRD FE MMS MAT1";

        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;

        /// <summary>
        ///     Will be generated new for each test
        /// </summary>
        private ApiPersonProfileV3 testUser = null!;
        private readonly ApiPersonProfileV3 resourceOwnerPerson;
        private readonly ApiPersonProfileV3 requestAssignedPerson;
        private ApiPositionV2 taskOwnerPosition = null!;
        private ApiPositionV2 requestPosition = null!;


        // Created by the async lifetime
        private FusionTestProjectBuilder testProject = null!;

        private Guid ProjectId => testProject.Project.ProjectId;


        public InternalResourceAllocationNotificationTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test users
            resourceOwnerPerson = fixture.AddProfile(FusionAccountType.Employee);
            resourceOwnerPerson.IsResourceOwner = true;
            requestAssignedPerson = fixture.AddProfile(FusionAccountType.Employee);


            fixture.EnsureDepartment(TestDepartmentId);
            LineOrgServiceMock.AddTestUser().MergeWithProfile(requestAssignedPerson).WithManager(resourceOwnerPerson).WithFullDepartment("TPD PRD FE MMS STR2").SaveProfile();
        }

        private HttpClient Client => fixture.ApiFactory.CreateClient();

        public Task InitializeAsync()
        {
            // Mock profile
            testUser = fixture.AddResourceOwner(TestDepartmentId);

            // Mock project
            testProject = new FusionTestProjectBuilder()
                .AddToMockService();

            taskOwnerPosition = testProject.AddPosition().WithAssignedPerson(testUser);
            requestPosition = testProject.AddPosition().WithAssignedPerson(requestAssignedPerson).WithTaskOwner(taskOwnerPosition.Id);
            testProject.SetTaskOwner(requestPosition.Id, taskOwnerPosition.Id);
            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(testProject.Project);

            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
        #region Notification tests

        [Fact]
        public async Task DirectRequest_DeleteRequest_ShouldSendNotification()
        {
            using var adminScope = fixture.AdminScope();
            var directRequest = await Client.CreateRequestAsync(ProjectId, r => r
                .AsTypeDirect()
                .WithPosition(requestPosition)
                .WithProposedPerson(testUser)
                .WithAssignedDepartment(testUser.FullDepartment!));

            NotificationClientMock.SentMessages.Clear();
            await Client.TestClientDeleteAsync($"/resources/requests/internal/{directRequest.Id}");

            var taskOwner = directRequest.TaskOwner!.Persons!.First().AzureUniquePersonId.ToString();
            NotificationClientMock.SentMessages.Count(x => x.PersonIdentifier == taskOwner).Should().Be(1);
        }


        [Fact]
        public async Task DirectRequest_StartWorkFlow_ShouldNotify()
        {
            using var adminScope = fixture.AdminScope();
            var directRequest = await Client.CreateRequestAsync(ProjectId, r => r
                .AsTypeDirect()
                .WithPosition(requestPosition)
                .WithProposedPerson(testUser)
                .WithAssignedDepartment(testUser.FullDepartment!));

            NotificationClientMock.SentMessages.Clear();
            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{directRequest.Id}/start", null);
            response.Should().BeSuccessfull();

            var creator = response.Value.CreatedBy.AzureUniquePersonId.ToString();
            var taskOwner = directRequest.TaskOwner!.Persons!.First().AzureUniquePersonId.ToString();

            DumpNotificationsToLog(NotificationClientMock.SentMessages);
            NotificationClientMock.SentMessages.Count(x => x.PersonIdentifier == creator).Should().Be(0);
            NotificationClientMock.SentMessages.Count(x => x.PersonIdentifier == taskOwner).Should().Be(1);
        }

        private static void DumpNotificationsToLog(List<NotificationClientMock.Notification> sentMessages)
        {
            foreach (var notification in sentMessages)
            {
                TestLogger.TryLog($"PersonIdentifier:{notification.PersonIdentifier}, Title:{notification.Title}");
            }

        }

        [Fact]
        public async Task JointVentureRequest_StartWorkFlow_ShouldNotify()
        {
            // Arrange
            using var adminScope = fixture.AdminScope();
            var jointVentureRequest = await Client.CreateRequestAsync(ProjectId, r => r.AsTypeJointVenture().WithPosition(requestPosition));
            var proposedPerson = new { ProposedPersonAzureUniqueId = testUser.AzureUniqueId };
            var patchPerson = await Client.TestClientPatchAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{jointVentureRequest.Id}", proposedPerson);
            patchPerson.Should().BeSuccessfull();
            NotificationClientMock.SentMessages.Clear();

            // Act
            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{jointVentureRequest.Id}/start", null);
            response.Should().BeSuccessfull();

            // Assert
            var creator = response.Value.CreatedBy.AzureUniquePersonId.ToString();
            var taskOwner = patchPerson.Value.TaskOwner!.Persons!.First().AzureUniquePersonId.ToString();

            DumpNotificationsToLog(NotificationClientMock.SentMessages);
            NotificationClientMock.SentMessages.Count(x => x.PersonIdentifier == creator).Should().Be(0);
            NotificationClientMock.SentMessages.Count(x => x.PersonIdentifier == taskOwner).Should().Be(1);
        }

        [Fact]
        public async Task NormalRequest_StartWorkFlow_ShouldNotify()
        {
            // Arrange
            using var adminScope = fixture.AdminScope();
            var normalRequest = await Client.CreateRequestAsync(ProjectId, r => r.AsTypeNormal().WithPosition(requestPosition));
            NotificationClientMock.SentMessages.Clear();

            // Act
            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{normalRequest.Id}/start", null);
            response.Should().BeSuccessfull();


            // Assert
            var creator = response.Value.CreatedBy.AzureUniquePersonId.ToString();
            var taskOwner = normalRequest.TaskOwner!.Persons!.First().AzureUniquePersonId.ToString();

            DumpNotificationsToLog(NotificationClientMock.SentMessages);
            NotificationClientMock.SentMessages.Count(x => x.PersonIdentifier == creator).Should().Be(0);
            NotificationClientMock.SentMessages.Count(x => x.PersonIdentifier == taskOwner).Should().Be(1);
        }

        [Fact]
        public async Task NormalRequest_WhenResourceOwnerIsProposingPerson_ShouldNotifyTaskOwnerAndCreator()
        {
            // Arrange
            using var adminScope = fixture.AdminScope();
            var normalRequest = await Client.CreateRequestAsync(ProjectId, r => r.AsTypeNormal().WithPosition(requestPosition));
            var proposedPerson = new { ProposedPersonAzureUniqueId = testUser.AzureUniqueId };
            var patchPerson = await Client.TestClientPatchAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{normalRequest.Id}", proposedPerson);
            patchPerson.Should().BeSuccessfull();

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{normalRequest.Id}/start", null);
            response.Should().BeSuccessfull();
            NotificationClientMock.SentMessages.Clear();

            var resourceOwner = PeopleServiceMock.AddTestProfile().WithRoles("Fusion.Resources.FullControl").SaveProfile();
            using var resourceOwnerScope = fixture.UserScope(resourceOwner);

            // Act
            var response2 = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{normalRequest.Id}/approve", null);
            response2.Should().BeSuccessfull();

            // Assert
            var creator = response.Value.CreatedBy.AzureUniquePersonId.ToString();
            var taskOwner = normalRequest.TaskOwner!.Persons!.First().AzureUniquePersonId.ToString();

            DumpNotificationsToLog(NotificationClientMock.SentMessages);
            NotificationClientMock.SentMessages.Count(x => x.PersonIdentifier == creator).Should().Be(1);
            NotificationClientMock.SentMessages.Count(x => x.PersonIdentifier == taskOwner).Should().Be(1);
        }
        #endregion
    }

}