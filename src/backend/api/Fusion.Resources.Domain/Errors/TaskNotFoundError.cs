using System;

namespace Fusion.Resources
{
    public class TaskNotFoundError : Exception
    {
        public TaskNotFoundError(Guid requestId, Guid taskId) : base($"Task with id '{taskId}' was not found on request with id '{requestId}'.")
        {
            RequestId = requestId;
            TaskId = taskId;
        }

        public Guid RequestId { get; }
        public Guid TaskId { get; }
    }
}
