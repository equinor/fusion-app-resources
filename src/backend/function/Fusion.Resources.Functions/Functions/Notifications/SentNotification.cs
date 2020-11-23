using Microsoft.Azure.Cosmos.Table;

namespace Fusion.Resources.Functions.Functions.Notifications
{
    public class SentNotification : TableEntity
    {
        public string RequestId => PartitionKey;

        public string Recipient => RowKey;

        /// <summary>
        /// The state the request was in when notified
        /// </summary>
        public string State { get; set; }
    }
}
