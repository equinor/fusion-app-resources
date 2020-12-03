using Fusion.ApiClients.Org;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.Functions.Notifications;
using Fusion.Testing.Mocks.OrgService;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Functions.Tests
{
    /// <summary>
    /// Contains mocks of services for the sender and the sender itself with those mocks constructed.
    /// Manipulate the mocks directly or use convenience methods to create test data and verification scenarioes.
    /// </summary>
    internal class IsolatedNotificationSender
    {
        public Mock<IResourcesApiClient> ResourcesMock;
        public Mock<INotificationApiClient> NotificationsMock;
        public Mock<ISentNotificationsTableClient> TableMock;
        public Mock<IOrgApiClient> OrgClientMock;
        public RequestNotificationSender NotificationSender;

        public IsolatedNotificationSender()
        {
            var orgApiClientFactoryMock = new Mock<IOrgApiClientFactory>();
            OrgClientMock = new Mock<IOrgApiClient>();
            orgApiClientFactoryMock.Setup(oafm => oafm.CreateClient(ApiClientMode.Application)).Returns(OrgClientMock.Object);

            ResourcesMock = new Mock<IResourcesApiClient>();
            NotificationsMock = new Mock<INotificationApiClient>();
            NotificationsMock.Setup(n => n.GetSettingsForUser(It.IsAny<Guid>()))
                .ReturnsAsync(new INotificationApiClient.NotificationSettings(true, 60, true)); //default delay to 60 for all users unless bypassed
            TableMock = new Mock<ISentNotificationsTableClient>();
            var urlResolverMock = new Mock<IUrlResolver>();

            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var configMock = new Mock<IConfiguration>();
            var sectionMock = new Mock<IConfigurationSection>();
            sectionMock.SetupGet(sm => sm.Value).Returns("0");
            configMock.Setup(c => c.GetSection(It.IsAny<string>())).Returns(sectionMock.Object);

            var telemetryMock = InitializeMockTelemetryChannel();

            NotificationSender = new RequestNotificationSender(orgApiClientFactoryMock.Object, ResourcesMock.Object, NotificationsMock.Object, TableMock.Object, urlResolverMock.Object, loggerFactoryMock.Object, configMock.Object, telemetryMock);
        }

        private TelemetryClient InitializeMockTelemetryChannel()
        {
            // Application Insights TelemetryClient doesn't have an interface (and is sealed), need our own mock
            MockTelemetryChannel mockTelemetryChannel = new MockTelemetryChannel();
            TelemetryConfiguration mockTelemetryConfig = new TelemetryConfiguration
            {
                TelemetryChannel = mockTelemetryChannel,
                InstrumentationKey = Guid.NewGuid().ToString(),
            };

            TelemetryClient mockTelemetryClient = new TelemetryClient(mockTelemetryConfig);
            return mockTelemetryClient;
        }

        internal IResourcesApiClient.DelegatedRole CreateExternalDelegate(ApiProjectContractV2 testContract, int delayInMinutes)
        {
            var delegatedRole = new IResourcesApiClient.DelegatedRole
            {
                Classification = "External",
                Person = new IResourcesApiClient.Person { AzureUniquePersonId = Guid.NewGuid(), Mail = "external.user@contractor.com" }
            };

            ResourcesMock.Setup(r => r.RetrieveDelegatesForContractAsync(It.Is<IResourcesApiClient.ProjectContract>(c => c.Id == testContract.Id)))
                .ReturnsAsync(new List<IResourcesApiClient.DelegatedRole> { delegatedRole });

            SetDelayForUser(delegatedRole.Person.AzureUniquePersonId.GetValueOrDefault(), delayInMinutes);

            return delegatedRole;
        }

        internal IResourcesApiClient.DelegatedRole CreateInternalDelegate(IResourcesApiClient.ProjectContract testContract, int delayInMinutes)
        {
            var delegatedRole = new IResourcesApiClient.DelegatedRole
            {
                Classification = "Internal",
                Person = new IResourcesApiClient.Person { AzureUniquePersonId = Guid.NewGuid(), Mail = "internal.user@equinor.com" }
            };

            ResourcesMock.Setup(r => r.RetrieveDelegatesForContractAsync(testContract))
                .ReturnsAsync(new List<IResourcesApiClient.DelegatedRole> { delegatedRole });

            SetDelayForUser(delegatedRole.Person.AzureUniquePersonId.GetValueOrDefault(), delayInMinutes);

            return delegatedRole;
        }

        internal void SetDelayForUser(Guid azureId, int delayInMinutes)
        {
            NotificationsMock.Setup(n => n.GetSettingsForUser(azureId))
                .ReturnsAsync(new INotificationApiClient.NotificationSettings(true, delayInMinutes, true));
        }

        internal void SetRequestNotificationSent(Guid requestId, Guid personAzureId, string state)
        {
            TableMock.Setup(tm => tm.NotificationWasSentAsync(requestId, personAzureId, state)).ReturnsAsync(true);
        }

        internal IResourcesApiClient.PersonnelRequest CreateTestRequest(ApiProjectContractV2 testContract, DateTimeOffset lastActivity, string state)
        {
            var testRequest = new IResourcesApiClient.PersonnelRequest
            {
                Id = Guid.NewGuid(),
                LastActivity = lastActivity,
                State = state,
                Person = new IResourcesApiClient.PersonnelRequest.RequestPersonnel { Mail = "testMail@test.com", Name = "Test Testesen" },
                Position = new IResourcesApiClient.PersonnelRequest.RequestPosition { AppliesFrom = DateTime.Now.AddYears(-1), AppliesTo = DateTime.Now.AddYears(1), Name = "Unit tester" }
            };

            ResourcesMock.Setup(r => r.GetTodaysContractRequests(It.Is<IResourcesApiClient.ProjectContract>(c => c.Id == testContract.Id), state))
                .ReturnsAsync(new IResourcesApiClient.PersonnelRequestList { Value = new List<IResourcesApiClient.PersonnelRequest> { testRequest } });

            return testRequest;
        }

        internal ApiProjectContractV2 CreateTestContract()
        {
            var projectBuilder = new FusionTestProjectBuilder()
                .WithContract(c => c
                    .WithPositions()
                    .WithCompanyRep()
                    .WithContractRep()
                    .WithExternalCompanyRep()
                    .WithExternalContractRep());

            var contract = projectBuilder.ContractsWithPositions
                .FirstOrDefault()
                .Item1;

            //make sure a dummy person is assigned to the instances of the Rep position, to be able to send notifications
            contract.CompanyRep.Instances.ForEach(i => i.SetAssignedPerson(new ApiPersonProfileV3 { AzureUniqueId = Guid.NewGuid() }));
            contract.ContractRep.Instances.ForEach(i => i.SetAssignedPerson(new ApiPersonProfileV3 { AzureUniqueId = Guid.NewGuid() }));
            contract.ExternalCompanyRep.Instances.ForEach(i => i.SetAssignedPerson(new ApiPersonProfileV3 { AzureUniqueId = Guid.NewGuid() }));
            contract.ExternalContractRep.Instances.ForEach(i => i.SetAssignedPerson(new ApiPersonProfileV3 { AzureUniqueId = Guid.NewGuid() }));

            var testContract = new IResourcesApiClient.ProjectContract
            {
                Id = contract.Id,
                ProjectId = projectBuilder.Project.ProjectId,
                ProjectName = projectBuilder.Project.Name,
                ContractNumber = contract.ContractNumber,
                Name = contract.Name,
                CompanyRepPositionId = contract.CompanyRep?.Id,
                ContractResponsiblePositionId = contract.ContractRep?.Id,
                ExternalCompanyRepPositionId = contract.ExternalCompanyRep?.Id,
                ExternalContractResponsiblePositionId = contract.ExternalContractRep?.Id
            };

            ResourcesMock.Setup(r => r.GetProjectContractsAsync()).ReturnsAsync(new List<IResourcesApiClient.ProjectContract> { testContract });
            OrgClientMock.Setup(r => r.GetContractV2Async(It.Is<OrgProjectId>(op => op.ProjectId == projectBuilder.Project.ProjectId), contract.Id))
                .ReturnsAsync(contract);

            return contract;
        }

        public void AssertNotificationSent(Guid azureUniqueId, Func<Times> times)
        {
            NotificationsMock.Verify(n => n.PostNewNotificationAsync(azureUniqueId, It.IsAny<string>(), It.IsAny<string>(), INotificationApiClient.Priority.High), times);
        }
    }
}
