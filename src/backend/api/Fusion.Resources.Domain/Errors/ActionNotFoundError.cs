using System;

namespace Fusion.Resources
{
    public class ActionNotFoundError : Exception
    {
        public ActionNotFoundError(Guid requestId, Guid actionId) : base($"Action with id '{actionId}' was not found on request with id '{requestId}'.")
        {
            RequestId = requestId;
            ActionId = actionId;
        }

        public Guid RequestId { get; }
        public Guid ActionId { get; }
    }
}
