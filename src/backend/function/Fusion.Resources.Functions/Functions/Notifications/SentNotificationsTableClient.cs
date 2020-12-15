using Fusion.Resources.Functions.TableStorage;
using Fusion.Resources.Functions.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Notifications
{
    public class SentNotificationsTableClient : ISentNotificationsTableClient
    {
        private readonly TableStorageClient tableStorageClient;
        private readonly TelemetryClient telemetryClient;
        private readonly ILogger<SentNotificationsTableClient> log;
        private const string TableName = "ResourcesSentNotifications";

        public SentNotificationsTableClient(TableStorageClient tableStorageClient, TelemetryClient telemetryClient, ILoggerFactory loggerFactory)
        {
            this.tableStorageClient = tableStorageClient;
            this.telemetryClient = telemetryClient;
            log = loggerFactory.CreateLogger<SentNotificationsTableClient>();
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

        public async Task CleanupSentNotifications(DateTime dateCutoff)
        {
            log.LogInformation($"Cleaning up sent notifications older than '{dateCutoff:g}'");

            var table = await tableStorageClient.GetTableAsync(TableName);
            var filter = TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThan, dateCutoff);

            var query = new TableQuery<SentNotification>().Where(filter);
            var result = table.ExecuteQuery(query);
            var count = result.Count();

            bool hasFailedItems = false;

            //batch operations must be within the same partition key, naturally.
            var groups = result
                .GroupBy(o => o.PartitionKey)
                .ToList();

            foreach (var group in groups)
            {
                var deleteBatch = new TableBatchOperation();
                var operations = group.Select(i => TableOperation.Delete(i)).ToList();
                operations.ForEach(i => deleteBatch.Add(i));

                try
                {
                    await table.ExecuteBatchAsync(deleteBatch);
                    log.LogInformation($"Successfully cleaned items with partition key '{group.Key}'");
                }
                catch (Exception ex) //Continue attemting to delete other batches, issue might be transient. Error will be thrown to indicate non-success for function.
                {
                    log.LogError($"Delete operation failed for sent notifications. Message: {ex.Message}");
                    hasFailedItems = true;
                }
            }

            if (hasFailedItems)
            {
                throw new Exception("Delete operation failed completely or partially. Items will be picked up on next run.");
            }
        }
    }
}
