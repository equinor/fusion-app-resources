using System;
using AdaptiveCards;

namespace Fusion.Resources.Domain.Commands
{
    public class NotifyTaskOwner : TrackableRequest
    {
        public NotifyTaskOwner(Guid requestId, AdaptiveCard card)
        {
            this.RequestId = requestId;
            this.Card = card;
        }

        public Guid RequestId{ get; }
        public AdaptiveCard Card { get; }

    }
}