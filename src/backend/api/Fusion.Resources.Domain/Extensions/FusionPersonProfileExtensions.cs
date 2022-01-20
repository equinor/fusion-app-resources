using Fusion.Integration.Profile;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public static class FusionPersonProfileExtensions
    {
        public static DbAzureAccountStatus GetDbAccountStatus(this FusionPersonProfile profile)
        {
            if (profile.AzureUniqueId.HasValue && profile.InvitationStatus is null)
            {
                // As long as AzureUniqueId exists, we consider the account available.
                return DbAzureAccountStatus.Available;
            }


            return profile.InvitationStatus switch
            {
                InvitationStatus.Accepted => DbAzureAccountStatus.Available,
                InvitationStatus.Pending => DbAzureAccountStatus.InviteSent,
                _ => DbAzureAccountStatus.NoAccount
            };
        }
    }
}
