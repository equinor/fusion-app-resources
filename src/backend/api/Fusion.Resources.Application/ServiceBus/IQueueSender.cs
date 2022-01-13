using System.Threading.Tasks;

namespace Fusion.Resources
{
    public interface IQueueSender
    {
        Task SendMessageAsync(QueuePath queue, object message);
        Task SendMessageDelayedAsync(QueuePath queue, object message, int delayInSeconds);
    }

}
