using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    

    public class TestMessageBus
    {
        public static bool IsTemporarilyUnavailable = false;

        private static List<TestQueueMessage> allMessages = new List<TestQueueMessage>();
        private static List<TestQueueMessage> localMessages = new List<TestQueueMessage>();

        private static List<Task> queueExecutionTasks = new List<Task>();
        private static Dictionary<string, List<Func<ServiceBusMessage, Task>>> queueHandlers = new Dictionary<string, List<Func<ServiceBusMessage, Task>>>();

        private static object writeLock = new object();

        public Task PublishMessageAsync(string entityPath, ServiceBusMessage message)
        {
            if (IsTemporarilyUnavailable)
            {
                throw new Exception("Service unavailable simulation!");
            }

            var item = new TestQueueMessage
            {
                Path = entityPath,
                Message = message,
                BodyText = Encoding.UTF8.GetString(message.Body)
            };

            localMessages.Add(item);
            allMessages.Add(item);


            queueExecutionTasks.Add(Task.Run(async () =>
            {
                if (queueHandlers.ContainsKey(entityPath))
                {
                    var handlerTasks = queueHandlers[entityPath].Select(h => h(message)).ToList();
                    await Task.WhenAll(handlerTasks);
                }
            }));

            return Task.CompletedTask;
        }


        public void RegisterQueueHandler(string queuepath, Func<ServiceBusMessage, Task> callback)
        {
            lock (writeLock)
            {
                queueHandlers[queuepath] = new List<Func<ServiceBusMessage, Task>>() { callback };
            }
        }

        public void RegisterTopicHandler(string queuepath, Func<ServiceBusMessage, Task> callback)
        {
            lock (writeLock)
            {
                if (queueHandlers.ContainsKey(queuepath))
                    queueHandlers[queuepath].Add(callback);
                else
                    queueHandlers[queuepath] = new List<Func<ServiceBusMessage, Task>>() { callback };
            }
        }

        public static void Clear() => allMessages.Clear();

        public static IReadOnlyCollection<TestQueueMessage> GetAllMessages() => allMessages.AsReadOnly();
        public static List<T> GetAllMessagePayloadsForPerson<T>(Func<TestQueueMessage, bool> predicate, string aadPersonId, T payloadType)
        {
            var filtered = allMessages.Where(predicate).OrderBy(m => m.Message.ScheduledEnqueueTime);
            var result = new List<T>();

            foreach (var msg in filtered)
            {
                var cloudEvent = JsonConvert.DeserializeObject<Events.CloudEventV1<Events.People.PeopleSubscriptionEvent>>(msg.BodyText);


                var body = JsonConvert.DeserializeAnonymousType(cloudEvent.Data, new
                {
                    Person = new
                    {
                        AzureUniqueId = string.Empty,
                    },
                    Type = string.Empty,
                });

                if (body.Person.AzureUniqueId != aadPersonId)
                    continue;

                result.Add(JsonConvert.DeserializeAnonymousType(cloudEvent.Data, payloadType));
            }

            return result;
        }
        public static List<T> GetAllMessages<T>()
        {
            var filtered = allMessages.OrderBy(m => m.Message.ScheduledEnqueueTime);
            var result = new List<T>();

            foreach (var msg in filtered)
            {
                var cloudEvent = JsonConvert.DeserializeObject<Events.CloudEventV1<T>>(msg.BodyText);


                var body = JsonConvert.DeserializeAnonymousType(cloudEvent.Data, new
                {
                    Person = new
                    {
                        AzureUniqueId = string.Empty,
                    },
                    Type = string.Empty,
                });

                result.Add(cloudEvent.Payload);
            }

            return result;
        }


        public static Task FinishProcessingAsync()
        {
            return Task.WhenAll(queueExecutionTasks);
        }


        public IReadOnlyCollection<TestQueueMessage> GetLocalMessages() => localMessages.AsReadOnly();

        public List<T> GetLocalMessages<T>()
        {
            var filtered = localMessages.OrderBy(m => m.Message.ScheduledEnqueueTime);
            var result = new List<T>();

            foreach (var msg in filtered)
            {
                var cloudEvent = JsonConvert.DeserializeObject<Events.CloudEventV1<T>>(msg.BodyText);


                var body = JsonConvert.DeserializeAnonymousType(cloudEvent.Data, new
                {
                    Person = new
                    {
                        AzureUniqueId = string.Empty,
                    },
                    Type = string.Empty,
                });

                result.Add(cloudEvent.Payload);
            }

            return result;
        }
    }
}
