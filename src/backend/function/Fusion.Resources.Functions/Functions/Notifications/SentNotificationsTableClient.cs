using Fusion.Resources.Functions.TableStorage;
using Fusion.Resources.Functions.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions.Notifications
{
    public class SentNotificationsTableClient : ISentNotificationsTableClient
    {
        private readonly TableStorageClient tableStorageClient;
        private readonly TelemetryClient telemetryClient;
        private const string TableName = "ResourcesSentNotifications";

        public SentNotificationsTableClient(TableStorageClient tableStorageClient, TelemetryClient telemetryClient)
        {
            this.tableStorageClient = tableStorageClient;
            this.telemetryClient = telemetryClient;
        }

        public async Task<SentNotification> GetSentNotificationsAsync(Guid requestId, Guid recipientId, string state)
        {
            //check if particular request was notified already using table storage
            var queryFilter = $"PartitionKey eq '{requestId}' and RowKey eq '{recipientId}' and State eq '{state}'";
            var table = await tableStorageClient.GetTableAsync(TableName);

            var query = new TableQuery<SentNotification>().Where(queryFilter);
            var result = table.ExecuteQuery(query);

            return result?.FirstOrDefault();
        }

        public async Task<bool> NotificationWasSentAsync(Guid requestId, Guid recipientId, string state)
        {
            var result = await GetSentNotificationsAsync(requestId, recipientId, state);
            return result != null;
        }

        public async Task AddToSentNotifications(Guid requestId, Guid recipientId, string state)
        {
            var table = await tableStorageClient.GetTableAsync(TableName);
            var operation = TableOperation.InsertOrReplace(new SentNotification { PartitionKey = requestId.ToString(), RowKey = recipientId.ToString(), State = state });

            try
            {
                await table.ExecuteAsync(operation);
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                telemetryClient.TrackCritical($"Error occured when adding to Resouces sent notifications. Request: '{requestId}', Recipient: '{recipientId}', State: '{state}'");
                throw;
            }
        }
    }
}
