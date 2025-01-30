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
    // https://github.com/equinor/fusion-resource-allocation-apps/blob/0c8477f48021c594af20c0b1ba7b549b187e2e71/apps/org-admin/src/pages/ProjectPage/pages/EditPositionsPage/pages/TimelineViewPage/components/TimelineFilter/selectors/positionSelector.ts#L14
    public static List<TBNPosition> GetTBNPositionsStartingWithinThreeMonths(IEnumerable<ApiPositionV2> allProjectPositions, ICollection<IResourcesApiClient.ResourceAllocationRequest> activeRequestsForProject)
    {
        var nowDate = NowDate;
        var expiringDate = nowDate.AddMonths(3);

        var tbnPositions = new List<TBNPosition>();

        foreach (var position in allProjectPositions)
        {
            if (IsSupportPosition(position))
                continue;

            var expiringInstance = position.Instances
                .OrderBy(i => i.AppliesFrom)
                .Where(i => i.AppliesFrom < expiringDate) // hasDueWithinThreeMonths
                .Where(i => i.AssignedPerson is null) // TBN instance
                .Where(i => !HasActiveChangeRequest(i, activeRequestsForProject)) // No active change request
                .FirstOrDefault(i => nowDate <= i.AppliesTo);

            if (expiringInstance is null)
                continue;

            tbnPositions.Add(new TBNPosition(position, expiringInstance.AppliesFrom));
        }

        return tbnPositions;
    }

    private static bool HasActiveChangeRequest(ApiPositionInstanceV2 instance, IEnumerable<IResourcesApiClient.ResourceAllocationRequest> activeRequests)
    {
        var instanceRequest = activeRequests.FirstOrDefault(r => r.OrgPositionInstance?.Id == instance.Id);

        return instanceRequest is not null && !string.Equals(instanceRequest.State, "Completed", StringComparison.OrdinalIgnoreCase);
    }

    // https://github.com/equinor/fusion-resource-allocation-apps/blob/0c8477f48021c594af20c0b1ba7b549b187e2e71/apps/org-admin/src/pages/ProjectPage/utils.ts#L86
    private static bool IsSupportPosition(ApiPositionV2? position)
    {
        if (position is null || string.IsNullOrEmpty(position.BasePosition.Name))
            return false;
        var supportNames = new[] { "support", "advisor", "assistance" };
        return supportNames.Any(s => position.BasePosition.Name.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    public static List<ExpiringPosition> GetPositionAllocationsEndingNextThreeMonths(IEnumerable<ApiPositionV2> allProjectPositions)
    {
        var nowDate = NowDate;
        var expiringDate = nowDate.AddMonths(3);

        var expiringPositions = new List<ExpiringPosition>();

        foreach (var position in allProjectPositions)
        {
            position.Instances = position.Instances.OrderBy(i => i.AppliesFrom).ToList();
            if (position.Instances.Count == 0) // No instances, skip
                continue;

            var instancesWithinPeriod = FindInstancesWithinPeriod(position, new DateTimeRange() { Start = nowDate, Stop = expiringDate });


            var anyAllocatedWithinPeriod = instancesWithinPeriod.EndingWithinPeriod
                .Concat(instancesWithinPeriod.ContainedWithinPeriod)
                .Any(i => i.AssignedPerson is not null);

            if (!anyAllocatedWithinPeriod)
                continue;


            foreach (var instance in instancesWithinPeriod.Instances)
            {
                if ((instancesWithinPeriod.ContainedWithinPeriod.Contains(instance) || instancesWithinPeriod.StartingWithinPeriod.Contains(instance))
                    && instance.AssignedPerson is null)
                {
                    expiringPositions.Add(new ExpiringPosition(position, instance.AppliesTo));
                    break;
                }

                if (instancesWithinPeriod.AnyInstancesAfter(instance) == false)
                {
                    expiringPositions.Add(new ExpiringPosition(position, instance.AppliesTo));
                    break;
                }
            }
        }


        return expiringPositions;
    }

    #region AllocationsExpiringHelpers

    private static InstancesWithinPeriod FindInstancesWithinPeriod(ApiPositionV2 position, DateTimeRange period)
    {
        var instancesEndingWithinPeriod = position.Instances
            .Where(i => period.Contains(i.AppliesTo) && !period.Contains(i.AppliesFrom))
            .ToList();

        var instancesContainingPeriod = position.Instances
            .Where(i => period.Contains(i.AppliesFrom) && period.Contains(i.AppliesTo))
            .ToList();

        var instancesStartingWithinPeriod = position.Instances
            .Except(instancesContainingPeriod)
            .Where(i => period.Contains(i.AppliesFrom) && !period.Contains(i.AppliesTo))
            .ToList();

        var instancesStartingAfterPeriod = position.Instances
            .Where(i => i.AppliesFrom > period.Stop)
            .ToList();

        var lastInstance = instancesStartingAfterPeriod.LastOrDefault();


        return new InstancesWithinPeriod()
        {
            EndingWithinPeriod = instancesEndingWithinPeriod,
            ContainedWithinPeriod = instancesContainingPeriod,
            StartingWithinPeriod = instancesStartingWithinPeriod,
            LastInstance = lastInstance
        };
    }


    private class InstancesWithinPeriod
    {
        /// Last instance starting after period (outside of scope)
        public ApiPositionInstanceV2? LastInstance { get; set; }

        /// Starts outside of period and ends within period
        public required List<ApiPositionInstanceV2> EndingWithinPeriod { get; set; }

        /// Starts and ends within period
        public required List<ApiPositionInstanceV2> ContainedWithinPeriod { get; set; }

        /// Starting within period and ending after period
        public required List<ApiPositionInstanceV2> StartingWithinPeriod { get; set; }

        public List<ApiPositionInstanceV2> Instances => EndingWithinPeriod.Concat(ContainedWithinPeriod).Concat(StartingWithinPeriod).Distinct().ToList();

        public bool AnyInstancesAfter(ApiPositionInstanceV2 instance)
        {
            // In this case the instance outlasts the period and is outside of scope
            if (StartingWithinPeriod.Contains(instance))
                return true;

            return Instances.Except([instance]).Any(i => i.AppliesFrom > instance.AppliesTo);
        }
    }


    private class DateTimeRange
    {
        public DateTime Start { get; set; }
        public DateTime Stop { get; set; }

        public bool Overlaps(DateTimeRange other)
        {
            return Start < other.Stop && Stop > other.Start;
        }

        public bool Contains(DateTime dateTime)
        {
            return Start <= dateTime && dateTime <= Stop;
        }

        public TimeSpan Duration()
        {
            return Stop - Start;
        }
    }

    #endregion


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