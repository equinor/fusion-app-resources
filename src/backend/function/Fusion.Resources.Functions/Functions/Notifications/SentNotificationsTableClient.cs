using Fusion.Resources.Functions.TableStorage;
using Microsoft.Azure.Cosmos.Table;
using System;
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

        public async Task<SentNotifications> GetSentNotificationsAsync(Guid requestId, Guid recipientId)
        {
            //check if particular request was notified already using table storage
            var table = await tableStorageClient.GetTableAsync(TableName);
            var result = await table.GetByKeysAsync<SentNotifications>(requestId.ToString(), recipientId.ToString());

            return result;
        }

        public async Task<bool> NotificationWasSentAsync(Guid requestId, Guid recipientId)
        {
            var result = await GetSentNotificationsAsync(requestId, recipientId);
            return result != null;
        }

        public async Task AddToSentNotifications(Guid requestId, Guid recipientId)
        {
            var table = await tableStorageClient.GetTableAsync(TableName);
            var operation = TableOperation.InsertOrReplace(new SentNotifications { PartitionKey = requestId.ToString(), RowKey = recipientId.ToString() });
            await table.ExecuteAsync(operation);
        }
    }
}
