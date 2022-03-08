using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;

namespace Fusion.Resources.Domain
{
    public static class FusionTelemetryEvents
    {

        /// <summary>
        /// Notify that the system has encountered a severe incident/event that should be acted upon or investigated.
        /// </summary>
        public static void TrackFusionCriticalEvent(this TelemetryClient telemetryClient, string message)
        {
            var @event = CriticalEvent(message);
            telemetryClient.TrackEvent(@event);
        }
        /// <summary>
        /// Notify that the system has encountered a severe incident/event that should be acted upon or investigated.
        /// </summary>
        public static void TrackFusionCriticalEvent(this TelemetryClient telemetryClient, string message, Action<FusionEventBuilder> builder)
        {
            var @event = CriticalEvent(message, builder);
            telemetryClient.TrackEvent(@event);
        }

        /// <summary>
        /// Notify the system something that is relevant to know. 
        /// This info should not require any action to be taken.
        /// </summary>
        public static void TrackFusionImportantInfo(this TelemetryClient telemetryClient, string message, Action<FusionEventBuilder> builder)
        {
            var item = new FusionEventBuilder("Important Info", message);
            builder.Invoke(item);

            telemetryClient.TrackEvent(item.GetTelemetryItem());
        }

        public static EventTelemetry CriticalEvent(string message, Exception? ex = null)
        {
            return CriticalEvent(message, i => i.WithException(ex));
        }

        public static EventTelemetry CriticalEvent(string message, Action<FusionEventBuilder> builder)
        {
            var item = new FusionEventBuilder("Critical Event", message);
            builder.Invoke(item);
            return item.GetTelemetryItem();
        }

        public class FusionEventBuilder
        {            
            private readonly EventTelemetry eventTelemetry;
            public FusionEventBuilder(string type, string message)
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

            public FusionEventBuilder WithException(Exception? ex)
            {
                if (ex != null)
                {
                    eventTelemetry.Properties["ExceptionMessage"] = ex.Message;
                    eventTelemetry.Properties["ExceptionStack"] = ex.ToString();
                }

                return this;
            }

            public FusionEventBuilder WithProperty(string key, string value)
            {
                eventTelemetry.Properties[key] = value ?? string.Empty;
                return this;
            }

            public FusionEventBuilder WithProperty(string key, int value)
            {
                eventTelemetry.Properties[key] = $"{value}";
                return this;
            }

            public FusionEventBuilder WithProperty(string key, DateTime? value)
            {
                eventTelemetry.Properties[key] = value?.ToString("u") ?? string.Empty;
                return this;
            }
            public FusionEventBuilder WithProperty(string key, DateTimeOffset? value)
            {
                eventTelemetry.Properties[key] = value?.UtcDateTime.ToString("u") ?? string.Empty;
                return this;
            }

            public EventTelemetry GetTelemetryItem() => eventTelemetry;
        }

    }
}
