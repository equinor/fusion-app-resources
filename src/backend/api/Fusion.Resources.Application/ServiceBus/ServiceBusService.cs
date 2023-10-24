using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using System.Collections.Generic;

namespace Fusion.Resources.ServiceBus
{
    internal class ServiceBusQueueSender : IQueueSender
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<ServiceBusQueueSender> logger;
        private readonly ServiceBusClient? client;

        // Caching the sender is recommended when the application is publishing messages regularly or semi-regularly. The sender is responsible for ensuring efficient network, CPU, and memory use
        private Dictionary<string, ServiceBusSender> cachedSenders = new Dictionary<string, ServiceBusSender>();

        public ServiceBusQueueSender(IConfiguration configuration, ILogger<ServiceBusQueueSender> logger)
        {
            var connectionString = configuration.GetConnectionString("ServiceBus");

            // Leaving this as nullable so integration tests etc don't fail untill the functionality is required.
            if (!string.IsNullOrEmpty(connectionString))
            {
                this.client = new ServiceBusClient(connectionString);
            }

            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task SendMessageAsync(QueuePath queue, object message)
        {
            await SendMessageDelayedAsync(queue, message, 0);
        }

        public async Task SendMessageDelayedAsync(QueuePath queue, object message, int delayInSeconds)
        {
            if (!IsDisabled)
            {
                var jsonMessage = JsonSerializer.Serialize(message);

                var entityPath = ResolveQueuePath(queue);
                var queueSender = GetQueueSender(entityPath);
                
                var sbMessage = new ServiceBusMessage(jsonMessage) { ContentType = "application/json" };
                if (delayInSeconds > 0)
                    sbMessage.ScheduledEnqueueTime = DateTime.UtcNow.AddSeconds(delayInSeconds);

                logger.LogInformation($"Posting message to {entityPath}: {jsonMessage}");
                await queueSender.SendMessageAsync(sbMessage);                
            }
            else
            {
                logger.LogWarning("Sending queue messages has been disabled by config, ServiceBus:Disabled");
            }
        }

        private ServiceBusSender GetQueueSender(string queue)
        {
            if (client is null)
                throw new InvalidOperationException("Service bus has not been configured. Missing connection string.");

            if (cachedSenders.ContainsKey(queue))
            {
                return cachedSenders[queue];
            }

            cachedSenders[queue] = client.CreateSender(queue);
            return cachedSenders[queue];
        }

        /// <summary>
        /// Queue path should be configured in config. The config key should be the enum value.
        /// 
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        private string ResolveQueuePath(QueuePath queue)
        {
            var entityPath = configuration.GetValue<string>($"ServiceBus:Queues:{queue}");


            var entityPathOverride = configuration.GetValue<string>($"SERVICEBUS_QUEUES_{queue}");
            if (!string.IsNullOrEmpty(entityPathOverride))
                entityPath = entityPathOverride;

            if (string.IsNullOrEmpty(entityPath))
                entityPath = DefaultQueuePath(queue);

            logger.LogInformation($"Using service bus queue: {entityPath}");

            return entityPath;
        }

        private bool IsDisabled => configuration.GetValue<bool>("ServiceBus:Disabled", false);
        private string DefaultQueuePath(QueuePath queue) => queue switch
        {
            QueuePath.ProvisionPosition => "provision-position",
            _ => $"{queue}"
        };
    }

}
