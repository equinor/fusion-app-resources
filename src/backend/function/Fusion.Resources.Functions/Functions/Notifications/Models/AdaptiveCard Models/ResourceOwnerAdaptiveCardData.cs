using System.Collections.Generic;

namespace Fusion.Resources.Functions.Functions.Notifications.Models.AdaptiveCard_Models;

public class ResourceOwnerAdaptiveCardData
{
    public int TotalNumberOfRequests { get; set; }
    public int NumberOfOlderRequests { get; set; }
    public int NumberOfNewRequestsWithNoNomination { get; set; }
    public int NumberOfOpenRequests { get; set; }
    public List<string> PersonnelPositionsEndingWithNoFutureAllocation { get; set; } = new();
    public int PercentAllocationOfTotalCapacity { get; set; }
    public int NumberOfPersonnelAllocatedMoreThan100Percent { get; set; }
    public int NumberOfExtContractsEnding { get; set; }
}