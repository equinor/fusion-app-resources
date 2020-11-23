using Fusion.Resources.Functions.TableStorage;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Functions.Notifications
{
    public class SentNotificationsTableClient : ISentNotificationsTableClient
    {
        private readonly TableStorageClient tableStorageClient;
        private const string TableName = "ResourcesSentNotifications";

        public SentNotificationsTableClient(TableStorageClient tableStorageClient)
        {
            this.tableStorageClient = tableStorageClient;
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
            await table.ExecuteAsync(operation);
        }
    }
}
