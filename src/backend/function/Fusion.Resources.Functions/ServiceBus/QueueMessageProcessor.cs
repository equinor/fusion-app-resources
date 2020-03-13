using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Functions
{
    public class QueueMessageProcessor
    {
        private readonly ILogger log;
        private readonly MessageReceiver receiver;
        private readonly MessageSender sender;

        private const int retryCount = 5;


        public QueueMessageProcessor(ILogger log, MessageReceiver receiver, MessageSender sender)
        {
            this.log = log;
            this.receiver = receiver;
            this.sender = sender;
        }


        public async Task ProcessWithRetriesAsync(Message message, Func<string, ILogger, Task> action)
        {
            try
            {
                var messageBody = Encoding.UTF8.GetString(message.Body);
                log.LogInformation($"C# Queue trigger function processed: {messageBody}");

                log.LogInformation($"C# ServiceBus queue trigger function processed message sequence #{message.SystemProperties.SequenceNumber}");
                await action(messageBody, log);

                await receiver.CompleteAsync(message.SystemProperties.LockToken);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                log.LogInformation("Calculating exponential retry");

                // If the message doesn't have a retry-count, set as 0
                if (!message.UserProperties.ContainsKey("retry-count"))
                {
                    message.UserProperties["retry-count"] = 0;
                    message.UserProperties["original-SequenceNumber"] = message.SystemProperties.SequenceNumber;
                }

                // If there are more retries available
                if ((int)message.UserProperties["retry-count"] < retryCount)
                {
                    var retryMessage = message.Clone();
                    var retryCount = (int)message.UserProperties["retry-count"] + 1;
                    var interval = 10 * retryCount;
                    var scheduledTime = DateTimeOffset.Now.AddSeconds(interval);

                    retryMessage.UserProperties["retry-count"] = retryCount;
                    await sender.ScheduleMessageAsync(retryMessage, scheduledTime);
                    await receiver.CompleteAsync(message.SystemProperties.LockToken);                    

                    log.LogInformation($"Scheduling message retry {retryCount} to wait {interval} seconds and arrive at {scheduledTime.UtcDateTime}");
                    throw;
                }

                // If there are no more retries, deadletter the message (note the host.json config that enables this)
                else
                {
                    log.LogCritical($"Exhausted all retries for message sequence # {message.UserProperties["original-SequenceNumber"]}");
                    await receiver.DeadLetterAsync(message.SystemProperties.LockToken, "Exhausted all retries");
                    throw;
                }
            }
        }
    }
    
}
