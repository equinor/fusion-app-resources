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
        public async Task ProcessNotifications_ShouldNotifyCompanyRep_IfSet_WhenApprovedByExternalRequestIsPending()
        {
            var senderWithMocks = new IsolatedNotificationSender();
            var testContract = senderWithMocks.CreateTestContract();

            _ = senderWithMocks.CreateTestRequest(testContract, DateTime.UtcNow.AddHours(-2), "SubmittedToCompany");
            var companyRepInstance = testContract.CompanyRep.Instances.FirstOrDefault(i => i.AppliesFrom < DateTime.UtcNow.Date && i.AppliesTo > DateTime.UtcNow.Date);

            await senderWithMocks.NotificationSender.ProcessNotificationsAsync();

            //since we use bogus data, we might not get an active instance. If so, then no notification should be sent.
            if (companyRepInstance?.AssignedPerson?.AzureUniqueId != null)
            {
                senderWithMocks.AssertNotificationSent(companyRepInstance.AssignedPerson.AzureUniqueId.Value, Times.Once);
            }
        }

        [Fact]
        public async Task ProcessNotifications_ShouldNotifyContractRep_IfSet_WhenApprovedByExternalRequestIsPending()
        {
            var senderWithMocks = new IsolatedNotificationSender();
            var testContract = senderWithMocks.CreateTestContract();

            _ = senderWithMocks.CreateTestRequest(testContract, DateTime.UtcNow.AddHours(-2), "SubmittedToCompany");
            var contractRepInstance = testContract.ContractRep.Instances.FirstOrDefault(i => i.AppliesFrom < DateTime.UtcNow.Date && i.AppliesTo > DateTime.UtcNow.Date);

            await senderWithMocks.NotificationSender.ProcessNotificationsAsync();

            //since we use bogus data, we might not get an active instance. If so, then no notification should be sent.
            if (contractRepInstance?.AssignedPerson?.AzureUniqueId != null)
            {
                senderWithMocks.AssertNotificationSent(contractRepInstance.AssignedPerson.AzureUniqueId.Value, Times.Once);
            }
        }

        [Theory]
        [InlineData(90, 60)]
        [InlineData(21, 20)]
        [InlineData(900, 5)]
        public async Task ProcessNotifications_ShouldNotifyExternalCompanyRep_IfSet_WhenCreatedRequestIsPending(int minutesSinceLastActivity, int delay)
        {
            var senderWithMocks = new IsolatedNotificationSender();
            var testContract = senderWithMocks.CreateTestContract();

            _ = senderWithMocks.CreateTestRequest(testContract, DateTime.Now.AddMinutes(-minutesSinceLastActivity), "Created");
            var extCompRepInstance = testContract.ExternalCompanyRep.Instances.FirstOrDefault(i => i.AppliesFrom < DateTime.UtcNow.Date && i.AppliesTo > DateTime.UtcNow.Date);

            //since we use bogus data, we might not get an active instance. If so, then no notification should be sent.
            if (extCompRepInstance?.AssignedPerson?.AzureUniqueId != null)
            {
                senderWithMocks.SetDelayForUser(extCompRepInstance.AssignedPerson.AzureUniqueId.Value, delay);

                await senderWithMocks.NotificationSender.ProcessNotificationsAsync();

                senderWithMocks.AssertNotificationSent(extCompRepInstance.AssignedPerson.AzureUniqueId.Value, Times.Once);
            }
        }

        [Theory]
        [InlineData(90, 60)]
        [InlineData(21, 20)]
        [InlineData(900, 5)]
        public async Task ProcessNotifications_ShouldNotifyExternalContractRep_IfSet_WhenCreatedRequestIsPending(int minutesSinceLastActivity, int delay)
        {
            var senderWithMocks = new IsolatedNotificationSender();
            var testContract = senderWithMocks.CreateTestContract();

            //add a test request with last activity 2 hours ago
            _ = senderWithMocks.CreateTestRequest(testContract, DateTime.Now.AddMinutes(-minutesSinceLastActivity), "Created");
            var extContractRepInstance = testContract.ExternalContractRep.Instances.FirstOrDefault(i => i.AppliesFrom < DateTime.UtcNow.Date && i.AppliesTo > DateTime.UtcNow.Date);

            //since we use bogus data, we might not get an active instance. If so, then no notification should be sent.
            if (extContractRepInstance?.AssignedPerson?.AzureUniqueId != null)
            {
                senderWithMocks.SetDelayForUser(extContractRepInstance.AssignedPerson.AzureUniqueId.Value, delay);

                await senderWithMocks.NotificationSender.ProcessNotificationsAsync();

                senderWithMocks.AssertNotificationSent(extContractRepInstance.AssignedPerson.AzureUniqueId.Value, Times.Once);
            }
        }

        [Theory]
        [InlineData(90, 120)]
        [InlineData(1, 2)]
        [InlineData(2, 850)]
        public async Task ProcessNotifications_ShouldNOTNotifyDelegateRole_WhenRequestsAreNewerThanDelay(int minutesSinceLastActivity, int delay)
        {
            var senderWithMocks = new IsolatedNotificationSender();
            var testContract = senderWithMocks.CreateTestContract();

            //add a test request with last activity 2 hours ago
            _ = senderWithMocks.CreateTestRequest(testContract, DateTime.Now.AddMinutes(-minutesSinceLastActivity), "Created");

            //set delay to 2.5 hours, the requests should NOT be processed
            var delegatedRole = senderWithMocks.CreateExternalDelegate(testContract, delay);

            await senderWithMocks.NotificationSender.ProcessNotificationsAsync();

            senderWithMocks.AssertNotificationSent(delegatedRole.Person.AzureUniquePersonId.Value, Times.Never);
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
            senderWithMocks.AssertNotificationSent(delegatedRole.Person.AzureUniquePersonId.Value, Times.Never);
        }

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
            senderWithMocks.AssertNotificationSent(delegatedRole.Person.AzureUniquePersonId.Value, Times.Once);
        }
    }
}

