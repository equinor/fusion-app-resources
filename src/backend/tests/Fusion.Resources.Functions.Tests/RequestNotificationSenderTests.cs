using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fusion.Resources.Functions.Test
{
    public class RequestNotificationSenderTests
    {
        [Fact]
        public async Task ProcessNotifications_ShouldNotifyDelegateRole_WhenCreatedRequestIsPending()
        {
            var senderWithMocks = new IsolatedNotificationSender();
            var testContract = senderWithMocks.CreateTestContract();

            //add a test request with last activity 3 hours ago
            var testRequest = senderWithMocks.CreateTestRequest(testContract, DateTime.Now.AddHours(-3), "Created");

            //set delay to 2 hours, the requests should be processed
            var delegatedRole = senderWithMocks.CreateExternalDelegate(testContract, 120);

            await senderWithMocks.NotificationSender.ProcessNotificationsAsync();

            senderWithMocks.NotificationsMock.Verify(n => n
                .PostNewNotificationAsync(delegatedRole.Person.AzureUniquePersonId.GetValueOrDefault(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessNotifications_ShouldNotifyExternalCompanyRep_IfSet_WhenCreatedRequestIsPending()
        {
            var senderWithMocks = new IsolatedNotificationSender();
            var testContract = senderWithMocks.CreateTestContract();

            var extCompRepInstance = testContract.ExternalCompanyRep.Instances.FirstOrDefault(i => i.AppliesFrom < DateTime.UtcNow.Date && i.AppliesTo > DateTime.UtcNow.Date);

            await senderWithMocks.NotificationSender.ProcessNotificationsAsync();

            if (extCompRepInstance?.AssignedPerson?.AzureUniqueId != null)
            {
                senderWithMocks.NotificationsMock.Verify(n => n
                    .PostNewNotificationAsync(extCompRepInstance.AssignedPerson.AzureUniqueId.Value, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
            else
            {
                senderWithMocks.NotificationsMock.Verify(n => n.PostNewNotificationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public async Task ProcessNotifications_ShouldNotifyExternalContractRep_IfSet_WhenCreatedRequestIsPending()
        {
            var senderWithMocks = new IsolatedNotificationSender();
            var testContract = senderWithMocks.CreateTestContract();
            var extContractRepInstance = testContract.ExternalContractRep.Instances.FirstOrDefault(i => i.AppliesFrom < DateTime.UtcNow.Date && i.AppliesTo > DateTime.UtcNow.Date);

            await senderWithMocks.NotificationSender.ProcessNotificationsAsync();

            if (extContractRepInstance?.AssignedPerson?.AzureUniqueId != null)
            {
                senderWithMocks.NotificationsMock.Verify(n => n
                    .PostNewNotificationAsync(extContractRepInstance.AssignedPerson.AzureUniqueId.Value, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
            else
            {
                senderWithMocks.NotificationsMock.Verify(n => n.PostNewNotificationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }
        }

        [Fact]
        public async Task ProcessNotifications_ShouldNOTNotifyDelegateRole_WhenRequestsAreNewerThanDelay()
        {
            var senderWithMocks = new IsolatedNotificationSender();
            var testContract = senderWithMocks.CreateTestContract();

            //add a test request with last activity 2 hours ago
            _ = senderWithMocks.CreateTestRequest(testContract, DateTime.Now.AddHours(-2), "Created");

            //set delay to 2.5 hours, the requests should NOT be processed
            _ = senderWithMocks.CreateExternalDelegate(testContract, 150);

            await senderWithMocks.NotificationSender.ProcessNotificationsAsync();

            senderWithMocks.NotificationsMock.Verify(n => n.PostNewNotificationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ProcessNotifications_ShouldNOTNotifyDelegateRole_WhenNotificationForRequestsAlreadySent()
        {
            var senderWithMocks = new IsolatedNotificationSender();
            var testContract = senderWithMocks.CreateTestContract();

            //add a test request with last activity 2 hours ago
            var request = senderWithMocks.CreateTestRequest(testContract, DateTime.Now.AddHours(-2), "Created");

            //set delay to 1 hours, the requests should be processed
            var delegatedRole = senderWithMocks.CreateExternalDelegate(testContract, 60);

            //add it to the already sent list
            senderWithMocks.SetRequestNotificationSent(request.Id, delegatedRole.Person.AzureUniquePersonId.GetValueOrDefault());

            await senderWithMocks.NotificationSender.ProcessNotificationsAsync();

            senderWithMocks.NotificationsMock.Verify(n => n.PostNewNotificationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}

