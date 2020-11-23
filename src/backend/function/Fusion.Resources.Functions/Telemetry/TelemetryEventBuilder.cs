using Microsoft.ApplicationInsights.DataContracts;
using System;

namespace Fusion.Resources.Functions.Telemetry
{
    public class TelemetryEventBuilder
    {
        private readonly EventTelemetry eventTelemetry;

        public TelemetryEventBuilder(string type, string message)
        {
            eventTelemetry = new EventTelemetry(type);

            eventTelemetry.Properties["Message"] = message;

            // Try to get the service
            var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME");

            if (string.IsNullOrEmpty(serviceName))
                serviceName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");

            if (!string.IsNullOrEmpty(serviceName))
                eventTelemetry.Properties["Service"] = serviceName;

        }

        public TelemetryEventBuilder WithException(Exception ex)
        {
            if (ex != null)
            {
                eventTelemetry.Properties["ExceptionMessage"] = ex.Message;
                eventTelemetry.Properties["ExceptionStack"] = ex.ToString();
            }

            return this;
        }

        public TelemetryEventBuilder WithProperty(string key, string value)
        {
            eventTelemetry.Properties[key] = value ?? string.Empty;
            return this;
        }

        public TelemetryEventBuilder WithProperty(string key, int value)
        {
            eventTelemetry.Properties[key] = $"{value}";
            return this;
        }

        public TelemetryEventBuilder WithProperty(string key, DateTime? value)
        {
            eventTelemetry.Properties[key] = value?.ToString("u") ?? string.Empty;
            return this;
        }
        public TelemetryEventBuilder WithProperty(string key, DateTimeOffset? value)
        {
            eventTelemetry.Properties[key] = value?.UtcDateTime.ToString("u") ?? string.Empty;
            return this;
        }

        public EventTelemetry GetTelemetryItem() => eventTelemetry;

        public static TelemetryEventBuilder CriticalEvent(string message) => new TelemetryEventBuilder("Critical event", message);
    }
}
