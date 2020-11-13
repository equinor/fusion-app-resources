using Microsoft.Azure.Cosmos.Table;

namespace Fusion.Resources.Functions.Functions.Notifications
{
    public class SentNotifications : TableEntity
    {
        public string RequestId => PartitionKey;

        public string Recipient => RowKey;


    }
}
