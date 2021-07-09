using Fusion.Integration.Profile;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public partial class ProfileUpdated : INotification
    {
        public ProfileUpdated(FusionPersonProfile profile)
        {
            Profile = profile;
        }

        public FusionPersonProfile Profile { get; }
    }
}
