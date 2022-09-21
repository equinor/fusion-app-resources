using Microsoft.ApplicationInsights;

namespace Fusion.Resources.Functions.Telemetry
{
    public static class TelemetryClientExtensions
    {
        public static void TrackCritical(this TelemetryClient client, string message)
        {
            var @event = TelemetryEventBuilder.CriticalEvent(message);
            client.TrackEvent(@event.GetTelemetryItem());
        }
    }
}