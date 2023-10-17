using Azure.Messaging.ServiceBus;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    public class TestQueueMessage
    {
        public string Path { get; set; }
        public ServiceBusMessage Message { get; set; }
        public bool Processed { get; set; }
        public string BodyText { get; set; }
    }
}
