using System.Collections.Generic;

namespace Fusion.Resources.Functions.Functions.Notifications.Models.AdaptiveCards;

public class ResourceOwnerAdaptiveCardData
{
    public int TotalNumberOfPersonnel { get; set; }
    public int CapacityInUse { get; set; }
    public int NumberOfRequestsLastWeek { get; set; }
    public int NumberOfOpenRequests { get; set; }
    public int NumberOfRequestsStartingInMoreThanThreeMonths { get; set; }
    public int NumberOfRequestsStartingInLessThanThreeMonths { get; set; }
    public string AverageTimeToHandleRequests { get; set; }
    public int AllocationChangesAwaitingTaskOwnerAction { get; set; }
    public int ProjectChangesAffectingNextThreeMonths { get; set; }    
    internal IEnumerable<PersonnelContent> PersonnelPositionsEndingWithNoFutureAllocation { get; set; }

    internal IEnumerable<PersonnelContent> PersonnelAllocatedMoreThan100Percent { get; set; }
}