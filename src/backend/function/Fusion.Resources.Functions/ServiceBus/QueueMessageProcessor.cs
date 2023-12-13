using System;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Functions
{
    public class QueueMessageProcessor
    {
        private readonly ILogger log;
        private readonly ServiceBusMessageActions receiver;
        private readonly IAsyncCollector<ServiceBusMessage> sender;

        private const int MaxRetryCount = 5;


        public QueueMessageProcessor(ILogger log, ServiceBusMessageActions receiver, IAsyncCollector<ServiceBusMessage> sender)
        {
            this.log = log;
            this.receiver = receiver;
            this.sender = sender;
        }


        public async Task ProcessWithRetriesAsync(ServiceBusReceivedMessage message, Func<string, ILogger, Task> action)
        {
            try
            {
                var messageBody = Encoding.UTF8.GetString(message.Body);
                log.LogInformation($"C# Queue trigger function processed: {messageBody}");

                log.LogInformation($"C# ServiceBus queue trigger function processed message sequence #{message.SequenceNumber}");
                await action(messageBody, log);

                await receiver.CompleteMessageAsync(message);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                log.LogInformation("Calculating exponential retry");
                

                int retryCount = 0;
                long originalSequence = message.SequenceNumber;

                // If the message doesn't have a retry-count, set as 0
                if (message.ApplicationProperties.ContainsKey("retry-count"))
                {
                    retryCount = (int)message.ApplicationProperties["retry-count"];
                    originalSequence = (long)message.ApplicationProperties["original-SequenceNumber"];
                }

                // If there are more retries available
                if (retryCount < MaxRetryCount)
                {
                    var retryMessage = new ServiceBusMessage(message);
                    retryCount++;
                    var interval = 30 * retryCount;
                    var scheduledTime = DateTimeOffset.Now.AddSeconds(interval);

                    retryMessage.ApplicationProperties["retry-count"] = retryCount;
                    retryMessage.ApplicationProperties["original-SequenceNumber"] = originalSequence;
                    retryMessage.ScheduledEnqueueTime = scheduledTime;
                    await sender.AddAsync(retryMessage);
                    await receiver.CompleteMessageAsync(message);                    

                    log.LogInformation($"Scheduling message retry {retryCount} to wait {interval} seconds and arrive at {scheduledTime.UtcDateTime}");
                    throw;
                }

                // If there are no more retries, deadletter the message (note the host.json config that enables this)
                else
                {
                    log.LogCritical($"Exhausted all retries for message sequence # {message.ApplicationProperties["original-SequenceNumber"]}");
                    await receiver.DeadLetterMessageAsync(message, "Exhausted all retries");
                    throw;
                }
            }
        }
    }
    
}
