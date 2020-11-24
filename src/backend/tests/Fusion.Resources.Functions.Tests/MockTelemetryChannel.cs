using Microsoft.ApplicationInsights.Channel;
using System.Collections.Concurrent;

namespace Fusion.Resources.Functions.Tests
{
    public class MockTelemetryChannel : ITelemetryChannel
    {
        public ConcurrentBag<ITelemetry> SentTelemtries = new ConcurrentBag<ITelemetry>();
        public bool IsFlushed { get; private set; }
        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; }

        public void Send(ITelemetry item)
        {
            SentTelemtries.Add(item);
        }

        public void Flush()
        {
            IsFlushed = true;
        }

        public void Dispose()
        {

        }
    }
}
