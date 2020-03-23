using Fusion.Integration.Profile;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public static class FusionPersonProfileExtensions
    {
        public static DbAzureAccountStatus GetDbAccountStatus(this FusionPersonProfile profile)
        {
            switch (profile.InvitationStatus)
            {
                case InvitationStatus.Accepted: return DbAzureAccountStatus.Available;
                case InvitationStatus.Pending: return DbAzureAccountStatus.InviteSent;
                default: return DbAzureAccountStatus.NoAccount;
            }
        }
    }
}
