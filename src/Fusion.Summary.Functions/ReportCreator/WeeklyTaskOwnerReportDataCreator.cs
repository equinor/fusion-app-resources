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
    public static List<TBNPosition> GetTBNPositionsStartingWithinThreeMonths(IEnumerable<ApiPositionV2> allProjectPositions,
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

    public static List<ExpiringPosition> GetPositionAllocationsEndingNextThreeMonths(IEnumerable<ApiPositionV2> allProjectPositions)
    {
        /*
         * Remember that it's the position*Allocation* that is expiring, not the position itself.
         * So a position allocation can be considered expiring if:
         * 1. The position is active and the next split within the next 3 months does not have a person assigned
         *      - We want to notify task owners that an upcoming split is missing an allocation and that they should assign someone
         *
         * 2. The last split is expiring within the next 3 months.
         *    There can be more splits after this but if they're not starting (appliesFrom) within the next 3 months (from NowDate), we consider the position expiring.
         *    Once the later split comes within the 3-month window, the position will fall under TBNPositionsStartingWithinThreeMonths
         *      - We want to notify task owners that the position is expiring
         *
         * Note: If there is a gap between two splits and the gap/time-period is fully within the 3-month window,
         * then we DO NOT consider the position expiring. This is also an unusual case.
         */

        var nowDate = NowDate;
        var expiringDate = nowDate.AddMonths(3);

        var expiringPositions = new List<ExpiringPosition>();

        foreach (var position in allProjectPositions)
        {
            if (position.Instances.Count == 0) // No instances, skip
                continue;

            var activeInstance = position.Instances.FirstOrDefault(i => i.AppliesFrom <= nowDate && i.AppliesTo >= nowDate);


            // Find future instances with a start date within the 3-month window that may or may not end within the 3-month window
            var futureInstances = position.Instances
                .Where(i => i.AppliesFrom >= nowDate && i.AppliesFrom < expiringDate)
                .OrderBy(i => i.AppliesFrom)
                .ToList();

            // Handle case where the position is not currently active
            if (activeInstance is null)
            {
                if (futureInstances.Count == 0)
                    continue; // This is a past position

                var endingPositionAllocation = FindFirstTBNOrLastExpiringInstance(futureInstances);

                // If the last instance is not the last instance then there are more instances after it that are not within the 3-month window or TBN
                var isEndingInstanceLast = futureInstances.Last() == endingPositionAllocation;

                if (endingPositionAllocation is not null && isEndingInstanceLast)
                    expiringPositions.Add(new ExpiringPosition(position, endingPositionAllocation.AppliesTo));

                continue;
            }


            // Handle case where the position is currently active

            var isActiveInstanceExpiring = activeInstance.AppliesTo < expiringDate;

            if (isActiveInstanceExpiring && futureInstances.Count == 0)
            {
                expiringPositions.Add(new ExpiringPosition(position, activeInstance.AppliesTo));
                continue;
            }

            if (isActiveInstanceExpiring)
            {
                var endingPositionAllocation = FindFirstTBNOrLastExpiringInstance(futureInstances);

                if (endingPositionAllocation is not null)
                    expiringPositions.Add(new ExpiringPosition(position, endingPositionAllocation.AppliesTo));
            }

            // The instance is active and not expiring, continue to next position
        }


        return expiringPositions;
    }

    private static ApiPositionInstanceV2? FindFirstTBNOrLastExpiringInstance(IEnumerable<ApiPositionInstanceV2> futureOrderedInstances)
    {
        ApiPositionInstanceV2? lastExpiringInstance = null;
        foreach (var instance in futureOrderedInstances)
        {
            if (instance.AssignedPerson is null)
                return instance; // We found a TBN instance

            if (instance.AppliesTo < NowDate.AddMonths(3))
                lastExpiringInstance = instance; // We found an expiring instance
        }

        return lastExpiringInstance;
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