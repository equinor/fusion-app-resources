using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.Functions.Notifications;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Functions.Test
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
            TableMock = new Mock<ISentNotificationsTableClient>();

            var loggerMock = new Mock<ILogger>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            NotificationSender = new RequestNotificationSender(orgApiClientFactoryMock.Object, ResourcesMock.Object, NotificationsMock.Object, TableMock.Object, loggerFactoryMock.Object);
        }

        internal IResourcesApiClient.DelegatedRole CreateExternalDelegate(IResourcesApiClient.ProjectContract testContract, int delayInMinutes)
        {
            var delegatedRole = new IResourcesApiClient.DelegatedRole
            {
                Classification = "External",
                Person = new IResourcesApiClient.Person { AzureUniquePersonId = Guid.NewGuid(), Mail = "external.user@contractor.com" }
            };

            ResourcesMock.Setup(r => r.RetrieveDelegatesForContractAsync(testContract))
                .ReturnsAsync(new List<IResourcesApiClient.DelegatedRole> { delegatedRole });

            NotificationsMock.Setup(n => n.GetDelayForUserAsync(delegatedRole.Person.AzureUniquePersonId.GetValueOrDefault()))
                .ReturnsAsync(delayInMinutes);

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

            NotificationsMock.Setup(n => n.GetDelayForUserAsync(delegatedRole.Person.AzureUniquePersonId.GetValueOrDefault()))
                .ReturnsAsync(delayInMinutes);

            return delegatedRole;
        }

        internal void SetRequestNotificationSent(Guid requestId, Guid personAzureId)
        {
            TableMock.Setup(tm => tm.NotificationWasSentAsync(requestId, personAzureId)).ReturnsAsync(true);
        }

        internal IResourcesApiClient.PersonnelRequest CreateTestRequest(IResourcesApiClient.ProjectContract testContract, DateTimeOffset lastActivity, string state)
        {
            var testRequest = new IResourcesApiClient.PersonnelRequest
            {
                Id = Guid.NewGuid(),
                LastActivity = lastActivity,
                State = state,
                Person = new IResourcesApiClient.PersonnelRequest.RequestPersonnel { Mail = "testMail@test.com", Name = "Test Testesen" },
                Position = new IResourcesApiClient.PersonnelRequest.RequestPosition { AppliesFrom = DateTime.Now.AddYears(-1), AppliesTo = DateTime.Now.AddYears(1), Name = "Unit tester" }
            };

            ResourcesMock.Setup(r => r.GetTodaysContractRequests(testContract, state))
                .ReturnsAsync(new IResourcesApiClient.PersonnelRequestList { Value = new List<IResourcesApiClient.PersonnelRequest> { testRequest } });

            return testRequest;
        }

        internal IResourcesApiClient.ProjectContract CreateTestContract()
        {
            var testContract = new IResourcesApiClient.ProjectContract
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                ProjectName = "Test notifications project",
                ContractNumber = "123456",
                Name = "Test notifications contract",
                CompanyRepPositionId = Guid.NewGuid(),
                ContractResponsiblePositionId = Guid.NewGuid(),
                ExternalCompanyRepPositionId = Guid.NewGuid(),
                ExternalContractResponsiblePositionId = Guid.NewGuid()
            };

            ResourcesMock.Setup(r => r.GetProjectContractsAsync()).ReturnsAsync(new List<IResourcesApiClient.ProjectContract> { testContract });

            return testContract;
        }
    }
}
