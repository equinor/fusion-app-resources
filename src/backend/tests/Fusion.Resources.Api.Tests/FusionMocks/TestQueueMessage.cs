using Microsoft.Azure.ServiceBus;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    public class TestQueueMessage
    {
        public string Path { get; set; }
        public Message Message { get; set; }
        public bool Processed { get; set; }
        public string BodyText { get; set; }
    }
}
