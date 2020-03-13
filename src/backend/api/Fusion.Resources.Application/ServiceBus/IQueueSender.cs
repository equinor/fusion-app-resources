using System;
using System.Threading.Tasks;

namespace Fusion.Resources
{
    public interface IQueueSender
    {
        Task SendMessageAsync(QueuePath queue, object message);
    }

}
