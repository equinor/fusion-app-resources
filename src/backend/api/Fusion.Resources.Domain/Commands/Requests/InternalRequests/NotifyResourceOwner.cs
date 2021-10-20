using AdaptiveCards;

namespace Fusion.Resources.Domain.Commands
{
    public class NotifyResourceOwner : TrackableRequest
    {
        public NotifyResourceOwner(string assignedDepartment, AdaptiveCard card, string notificationTitle)
        {
            this.AssignedDepartment = assignedDepartment;
            this.Card = card;
            this.NotificationTitle = notificationTitle;
        }

        public string AssignedDepartment { get; }
        public string NotificationTitle { get; }
        public AdaptiveCard Card { get; }

    }
}