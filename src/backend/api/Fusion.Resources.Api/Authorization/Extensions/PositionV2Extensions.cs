using Fusion.ApiClients.Org;
using System;
using System.Linq;

namespace Fusion.Resources.Api.Authorization.Handlers
{
    /// <summary>
    /// Should move to integration lib.
    /// </summary>
    public static class PositionV2Extensions
    {
        public static bool HasActiveAssignment(this ApiPositionV2? position, Guid azureUniqueId)
        {
            if (position == null)
                return false;

            if (position.Instances == null)
                return false;

            return position.Instances.Any(i => i.AssignedPerson?.AzureUniqueId == azureUniqueId && (i.AppliesFrom <= DateTime.Today && i.AppliesTo >= DateTime.Today));
        }
    }
}
