using Fusion.Events;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    public class EventNotificationClientMock : IEventNotificationClient
    {
        private readonly TestMessageBus bus;
        private readonly string entityPath;

        public EventNotificationClientMock(TestMessageBus bus, string entityPath)
        {
            this.bus = bus;
            this.entityPath = entityPath;
        }

        public Task SendNotificationAsync<T>(FusionEventType type, T payload) => SendNotificationAsync(type, payload, $"{Guid.NewGuid()}");
        public Task SendNotificationAsync<T>(FusionEventType type, T payload, string eventId) => DispatchNotification(type, null, payload, null, eventId);

        public Task SendNotificationAsync<T>(FusionEvent<T> @event) => DispatchNotification(@event.Type, @event.Category, @event.Payload, @event.AppContext, @event.EventId);

        public Task SendNotificationAsync<T>(FusionEventType type, T payload, Action<FusionEvent<T>> setup)
        {
            var builder = new FusionEvent<T>(type, payload);
            setup(builder);

            return DispatchNotification(builder.Type, builder.Category, builder.Payload, builder.AppContext, builder.EventId);
        }

        private Task DispatchNotification<T>(FusionEventType type, FusionEventCategory category, T payload, string appContext, string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                eventId = $"{Guid.NewGuid()}";

            var cEvent = new CloudEventV1<T>("test-client", type.Name, eventId, payload)
            {
                FusionCategory = category?.Name
            };


            var messageBody = JsonConvert.SerializeObject(cEvent);
            var sbMessage = new Message(Encoding.UTF8.GetBytes(messageBody))
            {
                MessageId = $"{type.Name}-{cEvent.Id}"
            };

            sbMessage.UserProperties.Add("type", type.Name);

            if (!string.IsNullOrEmpty(appContext))
            {
                sbMessage.UserProperties.Add("app", appContext);
            }

            return bus.PublishMessageAsync(entityPath, sbMessage);
        }
    }

}
