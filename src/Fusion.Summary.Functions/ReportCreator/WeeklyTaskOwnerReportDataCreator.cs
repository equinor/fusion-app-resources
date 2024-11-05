using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Summary.Functions.ReportCreator;

public abstract class WeeklyTaskOwnerReportDataCreator
{
    public static DateTime NowDate { get; set; }

    // Logic taken/inspired from the frontend
    // https://github.com/equinor/fusion-resource-allocation-apps/blob/a9330b2aa8d104e51536692a72334252d5e474e1/apps/org-admin/src/pages/ProjectPage/components/ChartComponent/components/utils.ts#L28
    public static List<TBNPosition> GetTBNPositionsStartingWithinThreeMonts(IEnumerable<ApiPositionV2> allProjectPositions,
        ICollection<IResourcesApiClient.ResourceAllocationRequest> requests)
    {
        var nowDate = NowDate;

        var tbnPositions = new List<TBNPosition>();

        foreach (var position in allProjectPositions)
        {
            if (IsSupportPosition(position))
                continue;

            var isPositionActive = position.Instances.Any(i => i.AppliesFrom.Date <= nowDate.Date && i.AppliesTo.Date >= nowDate.Date);

            if (isPositionActive)
                continue;

            var futureInstances = position.Instances.Where(i => i.AppliesFrom.Date >= nowDate.Date).ToList();

            if (futureInstances.Count == 0)
                continue;

            var startingInstance = futureInstances.MinBy(i => i.AppliesFrom); // Get the instance starting soonest

            if (startingInstance is null)
                continue;

            // TODO: In the fronted they dont show the warning if the instance has a resource allocation request
            // I'm wondering if we should do the same here...
            // If we don't do it here then it will be inconsistent with the frontend
            var instanceHasPersonalRequest = requests.Any(r => r.OrgPositionInstance?.Id == startingInstance.Id);

            if (instanceHasPersonalRequest)
                continue;

            if (startingInstance.AppliesFrom.Date < nowDate.AddMonths(3).Date && startingInstance.AssignedPerson is null)
                tbnPositions.Add(new TBNPosition(position, startingInstance.AppliesFrom));
        }

        return tbnPositions;
    }

    // https://github.com/equinor/fusion-resource-allocation-apps/blob/0c8477f48021c594af20c0b1ba7b549b187e2e71/apps/org-admin/src/pages/ProjectPage/utils.ts#L86
    private static bool IsSupportPosition(ApiPositionV2 position)
    {
        var supportNames = new[] { "support", "advisor", "assistance" };
        return supportNames.Any(s => position.Name.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    public static List<ExpiringPosition> GetPositionsEndingNextThreeMonths(IEnumerable<ApiPositionV2> allProjectPositions)
    {
        var nowDate = NowDate;

        var expiringPositions = new List<ExpiringPosition>();

        foreach (var position in allProjectPositions)
        {
            var isPositionActiveNow = position.Instances.Any(i => i.AppliesFrom.Date <= nowDate.Date && i.AppliesTo.Date >= nowDate.Date);

            if (!isPositionActiveNow)
                continue;

            var endingInstance = position.Instances.MaxBy(i => i.AppliesTo);

            if (endingInstance is null)
                continue;

            if (endingInstance.AppliesTo.Date < nowDate.AddMonths(3).Date)
                expiringPositions.Add(new ExpiringPosition(position, endingInstance.AppliesTo));
        }


        return expiringPositions;
    }

    // https://github.com/equinor/fusion-resource-allocation-apps/blob/0c8477f48021c594af20c0b1ba7b549b187e2e71/apps/org-admin/src/pages/ProjectPage/utils.ts#L53
    public static int GetActionsAwaitingTaskOwnerAsync(IEnumerable<IResourcesApiClient.ResourceAllocationRequest> requests)
    {
        return requests
            .Where(r => r.State is not null && !r.State.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            .Count(r => (r.HasProposedPerson && !r.State!.Equals("Created", StringComparison.OrdinalIgnoreCase) && !r.IsDraft) || r.Type == "ResourceOwnerChange");
    }

    public static List<PersonAdmin> GetExpiringAdmins(IEnumerable<PersonAdmin> activeAdmins)
    {
        var now = NowDate;

        return activeAdmins.Where(a => a.ValidTo <= now.AddMonths(3)).ToList();
    }
}

public record PersonAdmin(Guid AzureUniqueId, string FullName, DateTime ValidTo);

public record ExpiringPosition(ApiPositionV2 Position, DateTime ExpiresAt);

public record TBNPosition(ApiPositionV2 Position, DateTime StartsAt);