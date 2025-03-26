using Azure.Messaging.ServiceBus;
using Bogus;
using Fusion.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    public sealed class FakeNotificationTransaction : IEventNotificationTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task CommitPartialAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public Task RollbackAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }

    public class EventNotificationClientMock : IEventNotificationClient
    {
        private readonly TestMessageBus bus;
        private readonly string entityPath;

        public EventNotificationClientMock(TestMessageBus bus, string entityPath)
        {
            this.bus = bus;
            this.entityPath = entityPath;

            
        }

        public IEventNotificationTransaction CurrentTransaction => null;

        public Task<IEventNotificationTransaction> BeginTransactionAsync() => Task.FromResult<IEventNotificationTransaction>(new FakeNotificationTransaction());

        public Task SendScheduledNotificationAsync<T>(FusionEventType type, T payload, string eventId, TimeSpan? delay = null)
        {
            throw new NotImplementedException();
        }

        public Task SendNotificationAsync<T>(FusionEventType type, T payload) => SendNotificationAsync(type, payload, $"{Guid.NewGuid()}");

        public Task SendScheduledNotificationAsync<T>(FusionEventType type, T payload, TimeSpan? delay = null)
        {
            throw new NotImplementedException();
        }

        public Task SendNotificationAsync<T>(FusionEventType type, T payload, string eventId) => DispatchNotification(type, null, payload, null, eventId);

        public Task SendNotificationAsync<T>(FusionEvent<T> @event) => DispatchNotification(@event.Type, @event.Category, @event.Payload, @event.AppContext, @event.EventId);

        public Task SendScheduledNotificationAsync<T>(FusionEvent<T> @event, TimeSpan? delay = null)
        {
            throw new NotImplementedException();
        }

        public Task SendScheduledNotificationAsync(IEnumerable<FusionEvent> events, TimeSpan? delay = null)
        {
            throw new NotImplementedException();
        }

        public Task SendNotificationAsync<T>(FusionEventType type, T payload, Action<FusionEvent<T>> setup)
        {
            var builder = new FusionEvent<T>(type, payload);
            setup(builder);

            return DispatchNotification(builder.Type, builder.Category, builder.Payload, builder.AppContext, builder.EventId);
        }

        public async Task SendNotificationAsync(IEnumerable<FusionEvent> events)
        {
            foreach (var @event in events) 
                await DispatchNotification(@event.Type, @event.Category, @event.Payload, @event.AppContext, @event.EventId);
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
            var sbMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody))
            {
                MessageId = $"{type.Name}-{cEvent.Id}"
            };

            sbMessage.ApplicationProperties.Add("type", type.Name);

            if (!string.IsNullOrEmpty(appContext))
            {
                sbMessage.ApplicationProperties.Add("app", appContext);
            }

            return bus.PublishMessageAsync(entityPath, sbMessage);
        }
    }

}
