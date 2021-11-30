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
                // External accounts should have a invitation status.
                return profile.AccountType == FusionAccountType.External ? DbAzureAccountStatus.NoAccount : DbAzureAccountStatus.Available;
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
