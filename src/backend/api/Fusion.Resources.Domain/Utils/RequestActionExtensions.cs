using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public static class RequestActionExtensions
    {
        public static async Task<List<QueryRequestAction>> AsQueryRequestActionsAsync(this List<DbRequestAction> result, IFusionProfileResolver profileResolver)
        {
            var uniqueIds = result.Select(action => action.AssignedTo?.AzureUniqueId)
                .Union(result.Select(action => action.ResolvedBy?.AzureUniqueId))
                .Union(result.Select(action => (Guid?)action.SentBy.AzureUniqueId))
            .Where(x => x.HasValue)
            .Select(x => new PersonIdentifier(x!.Value));

            var profiles = await profileResolver.ResolvePersonsAsync(uniqueIds);

            var profileLookup = profiles
                .Where(x => x.Success && x.Profile?.AzureUniqueId != null)
                .ToDictionary(x => x.Profile!.AzureUniqueId!.Value);

            var actions = new List<QueryRequestAction>();
            foreach (var dbAction in result)
            {
                var action = new QueryRequestAction(dbAction);
                if (profileLookup.ContainsKey(dbAction.SentBy.AzureUniqueId))
                {
                    action.SentBy = profileLookup[dbAction.SentBy.AzureUniqueId].Profile;
                }

                if (dbAction.ResolvedBy is not null && profileLookup.ContainsKey(dbAction.ResolvedBy.AzureUniqueId))
                {
                    action.ResolvedBy = profileLookup[dbAction.ResolvedBy.AzureUniqueId].Profile;
                }

                if (dbAction.AssignedTo is not null && profileLookup.ContainsKey(dbAction.AssignedTo.AzureUniqueId))
                {
                    action.AssignedTo = profileLookup[dbAction.AssignedTo.AzureUniqueId].Profile;
                }

                actions.Add(action);
            }

            return actions;
        }

        public static async Task<QueryRequestAction> AsQueryRequestActionAsync(this DbRequestAction action, IFusionProfileResolver profileResolver)
        {
            var uniqueIds = new[] {
                    action.AssignedTo?.AzureUniqueId,
                    action.ResolvedBy?.AzureUniqueId,
                    action.SentBy.AzureUniqueId
                }
                .Where(x => x.HasValue)
                .Select(x => new PersonIdentifier(x!.Value));

            var profiles = await profileResolver.ResolvePersonsAsync(uniqueIds);

            return new QueryRequestAction(action)
            {
                SentBy = profiles.FirstOrDefault(x => x.Success && x.Profile!.AzureUniqueId == action.SentBy.AzureUniqueId)?.Profile,
                AssignedTo = profiles.FirstOrDefault(x => x.Success && x.Profile!.AzureUniqueId == action.AssignedTo?.AzureUniqueId)?.Profile,
                ResolvedBy = profiles.FirstOrDefault(x => x.Success && x.Profile!.AzureUniqueId == action.ResolvedBy?.AzureUniqueId)?.Profile,
            };
        }
    }
}
