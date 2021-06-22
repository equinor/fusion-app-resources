using AdaptiveCards;

namespace Fusion.Resources.Domain.Commands
{
    public class NotifyResourceOwner : TrackableRequest
    {
        public NotifyResourceOwner(string assignedDepartment, AdaptiveCard card)
        {
            this.AssignedDepartment = assignedDepartment;
            this.Card = card;
        }

        public string AssignedDepartment{ get; }
        public AdaptiveCard Card { get; }

    }
}