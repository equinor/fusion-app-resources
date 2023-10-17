using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Fusion.Resources.ServiceBus
{
    internal class ServiceBusQueueSender : IQueueSender
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<ServiceBusQueueSender> logger;
        private ServiceBusClient? client;

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
                if (client is null)
                    throw new InvalidOperationException("Service bus has not been configured. Missing connection string.");

                var jsonMessage = JsonSerializer.Serialize(message);

                var entityPath = Resolve(queue);

                if (string.IsNullOrEmpty(entityPath))
                    throw new InvalidOperationException($"Could not resolve the serivce bus entity path for queue '{queue}'");

                var queueSender = client.CreateSender(entityPath);

                
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

        private string? Resolve(QueuePath queue)
        {
            var entityPath = configuration.GetValue<string>($"ServiceBus:Queues:{queue}", DefaultQueuePath(queue));

            var entityPathOverride = configuration.GetValue<string>($"SERVICEBUS_QUEUES_{queue}");
            if (!string.IsNullOrEmpty(entityPathOverride))
                entityPath = entityPathOverride;

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
