using Fusion.Resources.Domain;
using MediatR;
using System;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class RequestStateChanged : INotification
        {
            public RequestStateChanged(Guid requestId, InternalRequestType type, string? fromState, string? toState)
            {
                RequestId = requestId;
                Type = type;
                FromState = fromState;
                ToState = toState;
            }

            public Guid RequestId { get; }
            public InternalRequestType Type { get; }
            public string? FromState { get; }
            public string? ToState { get; }
        }
    }

}