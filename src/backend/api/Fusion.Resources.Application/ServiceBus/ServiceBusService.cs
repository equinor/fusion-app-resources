using System;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fusion.Resources.ServiceBus
{
    internal class ServiceBusQueueSender : IQueueSender
    {
        private readonly string connectionString;
        private readonly IConfiguration configuration;
        private readonly ILogger<ServiceBusQueueSender> logger;

        public ServiceBusQueueSender(IConfiguration configuration, ILogger<ServiceBusQueueSender> logger)
        {
            connectionString = configuration.GetConnectionString("ServiceBus");
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
                var sender = GetClient(queue);
                var jsonMessage = JsonSerializer.Serialize(message);
                var queueMessage = new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(jsonMessage)) { ContentType = "application/json" };

                if (delayInSeconds > 0)
                {
                    queueMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddSeconds(delayInSeconds);
                }

                logger.LogInformation($"Posting message to {sender.Path}: {jsonMessage}");
                await sender.SendAsync(queueMessage);
            }
            else
            {
                logger.LogWarning("Sending queue messages has been disabled by config, ServiceBus:Disabled");
            }
        }

        private MessageSender GetClient(QueuePath queue)
        {
            var entityPath = configuration.GetValue<string>($"ServiceBus:Queues:{queue}", DefaultQueuePath(queue));

            var entityPathOverride = configuration.GetValue<string>($"SERVICEBUS_QUEUES_{queue}");
            if (!string.IsNullOrEmpty(entityPathOverride))
                entityPath = entityPathOverride;

            logger.LogInformation($"Using service bus queue: {entityPath}");

            var sender = new MessageSender(connectionString, entityPath);
            return sender;
        }

        private bool IsDisabled => configuration.GetValue<bool>("ServiceBus:Disabled", false);
        private string DefaultQueuePath(QueuePath queue) => queue switch
        {
            QueuePath.ProvisionPosition => "provision-position",
            _ => $"{queue}"
        };
    }

}
