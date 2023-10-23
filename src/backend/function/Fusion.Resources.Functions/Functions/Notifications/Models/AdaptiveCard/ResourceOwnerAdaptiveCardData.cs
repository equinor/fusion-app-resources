using System.Collections.Generic;

namespace Fusion.Resources.Functions.Functions.Notifications.Models.AdaptiveCard;

public class ResourceOwnerAdaptiveCardData
{
    public int TotalNumberOfRequests { get; set; }
    public int NumberOfOlderRequests { get; set; }
    public int NumberOfNewRequestsWithNoNomination { get; set; }
    public int NumberOfOpenRequests { get; set; }
    internal IEnumerable<PersonnelContent> PersonnelPositionsEndingWithNoFutureAllocation { get; set; }
    public int PercentAllocationOfTotalCapacity { get; set; }
    internal IEnumerable<PersonnelContent> PersonnelAllocatedMoreThan100Percent { get; set; }
    public int NumberOfExtContractsEnding { get; set; }
}