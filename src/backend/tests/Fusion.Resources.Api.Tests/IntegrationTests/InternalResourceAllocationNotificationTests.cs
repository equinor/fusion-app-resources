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
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
#nullable enable

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    [Collection("Integration")]
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
            requestPosition = testProject.AddPosition().WithAssignedPerson(requestAssignedPerson).WithTaskOwner(taskOwnerPosition.Id).WithLocation();
            testProject.SetTaskOwner(requestPosition.Id, taskOwnerPosition.Id);
            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(testProject.Project);

            NotificationClientMock.SentMessages.Clear();
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
        #region Notification tests

        [Fact]
        public async Task Request_Delete_ShouldNotify_WhenAssignedDepartment_WhenPublishedDraft()
        {
            using var adminScope = fixture.AdminScope();
            var request = await Client.CreateRequestAsync(ProjectId, r => r
                .AsTypeNormal()
                .WithPosition(requestPosition)
                .WithAssignedDepartment(testUser.FullDepartment!));

            await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{request.Id}/start", null);
            await Client.TestClientDeleteAsync($"/resources/requests/internal/{request.Id}");

            TestLogger.TryLog($"{JsonConvert.SerializeObject(new { request })}");
            DumpNotificationsToLog(NotificationClientMock.SentMessages);
            var notificationsForRequest = NotificationClientMock.SentMessages.GetNotificationsForRequestId(request.Id);
            notificationsForRequest.Count.Should().BeGreaterOrEqualTo(1);
        }

        [Fact]
        public async Task DirectRequest_StartWorkFlow_ShouldNotify()
        {
            using var adminScope = fixture.AdminScope();
            var directRequest = await Client.CreateRequestAsync(ProjectId, r => r
                .AsTypeDirect()
                .WithAdditionalNote("se separate description of tasks and skills for US IDM position.  The position should be an expatriate position until a suitable local candidate is available.   Task for US IDM.  Building an information management culture for wind projects in the US sector​  Establish new routines for information handling for new contract models, including handover to operations​  Establish a best practice for handling information and communication from stakeholders​  Networking to get to know American requirements, work culture and stakeholder management​  Ensure alignment and correct handling of information from permitting, stakeholder management and commercial disciplines​  Close collaboration with legal discipline to ensure correct handling of information with regards to local requirements​  Establish local IDM work processes for projects and operations​  Alignment with REN BD US​   Skillset .  Proven leadership experience and international experience​  Strong multi-discipline understanding​  Strong ability to identify the need for new processes, define requirements and, together with stakeholders, establish efficient solutions​  Methodical, analytical and structured problem solving​  Strong understanding of risk picture in the project and how this effects IDM deliveries​  Highly experienced in IDM​  Strong cultural collaboration skills​  Strong focus on good team collaboration and communication both with site team and home team​   Task on behalf of IDM home team.  Implement and follow-up information security and information management routines​  Solving ad-hoc issues regarding information security and information handling​  Emergency access management ​  Ensure alignment with the home team and PDC​  Ensure correct handling of authority communications​  Responsibility for follow up of site specific contract issues​  Training of internal and external personnel​  Hire and train local IDM resources​  Establish and maintain an archive for paper originals​  Leading by example​  Multi-discipline approach to the tasks​  ")
                .WithPosition(requestPosition)
                .WithProposedPerson(testUser)
                .WithAssignedDepartment(testUser.FullDepartment!));


            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{directRequest.Id}/start", null);
            response.Should().BeSuccessfull();

            var creator = response.Value.CreatedBy.AzureUniquePersonId.ToString();
            var taskOwner = directRequest.TaskOwner!.Persons!.First().AzureUniquePersonId.ToString();

            TestLogger.TryLog($"{JsonConvert.SerializeObject(new { creator, taskOwner, response.Value })}");
            DumpNotificationsToLog(NotificationClientMock.SentMessages);
            var notificationsForRequest = NotificationClientMock.SentMessages.GetNotificationsForRequestId(directRequest.Id);
            notificationsForRequest.Count(x => x.PersonIdentifier == creator).Should().Be(0);
            notificationsForRequest.Count(x => x.PersonIdentifier == taskOwner).Should().BeGreaterOrEqualTo(1);
        }

        private static void DumpNotificationsToLog(List<NotificationClientMock.Notification> sentMessages)
        {
            TestLogger.TryLog($"{JsonConvert.SerializeObject(sentMessages)}");
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


            // Act
            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{jointVentureRequest.Id}/start", null);
            response.Should().BeSuccessfull();

            // Assert
            var creator = response.Value.CreatedBy.AzureUniquePersonId.ToString();
            var taskOwner = patchPerson.Value.TaskOwner!.Persons!.First().AzureUniquePersonId.ToString();

            TestLogger.TryLog($"{JsonConvert.SerializeObject(new { creator, taskOwner, response.Value })}");
            DumpNotificationsToLog(NotificationClientMock.SentMessages);

            var notificationsForRequest = NotificationClientMock.SentMessages.GetNotificationsForRequestId(jointVentureRequest.Id);
            notificationsForRequest.Count(x => x.PersonIdentifier == creator).Should().Be(0);
            notificationsForRequest.Count(x => x.PersonIdentifier == taskOwner).Should().BeGreaterOrEqualTo(1);
        }

        [Fact]
        public async Task NormalRequest_StartWorkFlow_ShouldNotify()
        {
            // Arrange
            using var adminScope = fixture.AdminScope();
            var normalRequest = await Client.CreateRequestAsync(ProjectId, r => r.AsTypeNormal().WithPosition(requestPosition));


            // Act
            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{normalRequest.Id}/start", null);
            response.Should().BeSuccessfull();


            // Assert
            var creator = response.Value.CreatedBy.AzureUniquePersonId.ToString();
            var taskOwner = normalRequest.TaskOwner!.Persons!.First().AzureUniquePersonId.ToString();

            TestLogger.TryLog($"{JsonConvert.SerializeObject(new { creator, taskOwner, response.Value })}");
            DumpNotificationsToLog(NotificationClientMock.SentMessages);

            var notificationsForRequest = NotificationClientMock.SentMessages.GetNotificationsForRequestId(normalRequest.Id);
            notificationsForRequest.Count(x => x.PersonIdentifier == creator).Should().Be(0);
            notificationsForRequest.Count(x => x.PersonIdentifier == taskOwner).Should().BeGreaterOrEqualTo(1);
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


            var resourceOwner = PeopleServiceMock.AddTestProfile().WithRoles("Fusion.Resources.FullControl").SaveProfile();
            using var resourceOwnerScope = fixture.UserScope(resourceOwner);

            // Act
            var response2 = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{normalRequest.Id}/approve", null);
            response2.Should().BeSuccessfull();

            // Assert
            var creator = response.Value.CreatedBy.AzureUniquePersonId.ToString();
            var taskOwner = normalRequest.TaskOwner!.Persons!.First().AzureUniquePersonId.ToString();

            TestLogger.TryLog($"{JsonConvert.SerializeObject(new { creator, taskOwner, response2.Value })}");
            DumpNotificationsToLog(NotificationClientMock.SentMessages);

            var notificationsForRequest = NotificationClientMock.SentMessages.GetNotificationsForRequestId(normalRequest.Id);
            notificationsForRequest.Count(x => x.PersonIdentifier == creator).Should().Be(1);
            notificationsForRequest.Count(x => x.PersonIdentifier == taskOwner).Should().BeGreaterOrEqualTo(1);
        }

        [Fact]
        public async Task EnterpriseRequest_StartWorkFlow_ShouldNotNotifyTaskOwner()
        {
            // Arrange
            using var adminScope = fixture.AdminScope();
            var enterpriseRequest = await Client.CreateRequestAsync(ProjectId, r => r.AsTypeEnterprise().WithPosition(requestPosition).WithProposedPerson(testUser));

            // Act
            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/projects/{ProjectId}/requests/{enterpriseRequest.Id}/start", null);
            response.Should().BeSuccessfull();


            // Assert
            var creator = response.Value.CreatedBy.AzureUniquePersonId.ToString();
            var taskOwner = enterpriseRequest.TaskOwner!.Persons!.First().AzureUniquePersonId.ToString();

            TestLogger.TryLog($"{JsonConvert.SerializeObject(new { creator, taskOwner, response.Value })}");
            DumpNotificationsToLog(NotificationClientMock.SentMessages);

            var notificationsForRequest = NotificationClientMock.SentMessages.GetNotificationsForRequestId(enterpriseRequest.Id);

            notificationsForRequest.Count(x => x.PersonIdentifier == creator).Should().Be(0);
            notificationsForRequest.Count(x => x.PersonIdentifier == taskOwner && string.Equals(x.Title,
                "You have been assigned as resource owner for a personnel request", StringComparison.OrdinalIgnoreCase)).Should().Be(0);
        }
        #endregion
    }

}