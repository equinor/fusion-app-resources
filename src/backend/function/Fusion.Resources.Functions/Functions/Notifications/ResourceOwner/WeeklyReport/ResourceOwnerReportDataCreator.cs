using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Resources.Functions.ApiClients;

namespace Fusion.Resources.Functions.Functions.Notifications.ResourceOwner.WeeklyReport;

public abstract class ResourceOwnerReportDataCreator
{
    public static int GetTotalNumberOfPersonnel(
        IEnumerable<IResourcesApiClient.InternalPersonnelPerson> listOfInternalPersonnel)
    {
        return listOfInternalPersonnel.Count();
    }

    public static int GetCapacityInUse(
        List<IResourcesApiClient.InternalPersonnelPerson> listOfInternalPersonnel)
    {
        var actualWorkLoad = 0.0;
        var actualLeave = 0.0;
        foreach (var personnel in listOfInternalPersonnel)
        {
            actualWorkLoad += personnel.PositionInstances.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum();

            actualWorkLoad += personnel.EmploymentStatuses
                .Where(ab => ab.Type == IResourcesApiClient.ApiAbsenceType.OtherTasks && ab.IsActive)
                .Select(ab => ab.AbsencePercentage)
                .Sum() ?? 0;

            actualLeave += personnel.EmploymentStatuses
                .Where(ab =>
                    ab.Type is IResourcesApiClient.ApiAbsenceType.Absence
                        or IResourcesApiClient.ApiAbsenceType.Vacation && ab.IsActive)
                .Select(ab => ab.AbsencePercentage)
                .Sum() ?? 0;
        }

        var maximumPotentialWorkLoad = listOfInternalPersonnel.Count * 100;
        var potentialWorkLoad = maximumPotentialWorkLoad - actualLeave;
        if (potentialWorkLoad <= 0)
            return 0;
        var capacityInUse = actualWorkLoad / potentialWorkLoad * 100;
        if (capacityInUse < 0)
            return 0;

        return (int)Math.Round(capacityInUse);
    }

    public static int GetNumberOfRequestsLastWeek(
        IEnumerable<IResourcesApiClient.ResourceAllocationRequest> requests)
    {
        return requests
            .Count(req =>
                req.Type != null && !req.Type.Equals(RequestType.ResourceOwnerChange.ToString(),
                                     StringComparison.OrdinalIgnoreCase)
                                 && req.Created > DateTime.UtcNow.AddDays(-7) && !req.IsDraft);
    }

    public static int GetNumberOfOpenRequests(
        IEnumerable<IResourcesApiClient.ResourceAllocationRequest> requests)
        => requests.Count(req =>
            req.State != null && req.Type != null && !req.Type.Equals(RequestType.ResourceOwnerChange.ToString(),
                StringComparison.OrdinalIgnoreCase) &&
            !req.HasProposedPerson &&
            !req.State.Equals(RequestState.Completed.ToString(), StringComparison.OrdinalIgnoreCase));


    public static int GetNumberOfRequestsStartingInMoreThanThreeMonths(
        IEnumerable<IResourcesApiClient.ResourceAllocationRequest> requests)
    {
        var threeMonthsFromToday = DateTime.UtcNow.AddMonths(3);
        return requests
            .Count(x => x.Type != null &&
                        x.OrgPositionInstance != null &&
                        x.State != null &&
                        !x.State.Equals(RequestState.Completed.ToString(), StringComparison.OrdinalIgnoreCase) &&
                        !x.Type.Equals(RequestType.ResourceOwnerChange.ToString(),
                            StringComparison.OrdinalIgnoreCase) &&
                        x.OrgPositionInstance.AppliesFrom > threeMonthsFromToday);
    }

    public static int GetNumberOfRequestsStartingInLessThanThreeMonths(
        IEnumerable<IResourcesApiClient.ResourceAllocationRequest> requests)
    {
        var threeMonthsFromToday = DateTime.UtcNow.AddMonths(3);
        var today = DateTime.UtcNow;
        return requests
            .Count(x => x.Type != null &&
                        x.OrgPositionInstance != null &&
                        x.State != null &&
                        !x.State.Equals(RequestState.Completed.ToString(), StringComparison.OrdinalIgnoreCase) &&
                        !x.Type.Equals(RequestType.ResourceOwnerChange.ToString(),
                            StringComparison.OrdinalIgnoreCase) &&
                        x.OrgPositionInstance.AppliesFrom < threeMonthsFromToday &&
                        x.OrgPositionInstance.AppliesFrom > today && !x.HasProposedPerson);
    }

    public static string GetAverageTimeToHandleRequests(
        IEnumerable<IResourcesApiClient.ResourceAllocationRequest> requests)
    {
        var requestsHandledByResourceOwner = 0;
        var totalNumberOfDays = 0.0;
        var averageTimeToHandleRequests = "0";

        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

        var requestsLastThreeMonthsWithoutResourceOwnerChangeRequest = requests
            .Where(req => req.Created > threeMonthsAgo)
            .Where(r => r.Workflow is not null)
            .Where(_ => true)
            .Where(req => req.Type != null && !req.Type.Equals(RequestType.ResourceOwnerChange.ToString(),
                StringComparison.OrdinalIgnoreCase));

        foreach (var request in requestsLastThreeMonthsWithoutResourceOwnerChangeRequest)
        {
            if (request.State != null &&
                request.State.Equals(RequestState.Completed.ToString(), StringComparison.OrdinalIgnoreCase))
                continue;
            if (request.Workflow?.Steps is null)
                continue;

            var dateForCreation = request.Workflow.Steps
                .FirstOrDefault(step => step.Name.Equals("Created") && step.IsCompleted)?.Completed.Value.DateTime;
            var dateForApproval = request.Workflow.Steps
                .FirstOrDefault(step => step.Name.Equals("Proposed") && step.IsCompleted)?.Completed.Value.DateTime;
            if (dateForCreation == null || dateForApproval == null)
                continue;

            requestsHandledByResourceOwner++;
            var timespanDifference = dateForApproval - dateForCreation;
            var differenceInDays = timespanDifference.Value.TotalDays;
            totalNumberOfDays += differenceInDays;
        }

        if (!(totalNumberOfDays > 0))
            return averageTimeToHandleRequests;

        var averageAmountOfTimeDouble = totalNumberOfDays / requestsHandledByResourceOwner;
        var averageAmountOfTimeInt = Convert.ToInt32(averageAmountOfTimeDouble);

        averageTimeToHandleRequests = averageAmountOfTimeInt >= 1
            ? averageAmountOfTimeInt + " day(s)"
            : "Less than a day";

        return averageTimeToHandleRequests;
    }

    public static int GetAllocationChangesAwaitingTaskOwnerAction(
        IEnumerable<IResourcesApiClient.ResourceAllocationRequest> requests)
    {
        return requests
            .Where(req =>
                req.Type.Equals(RequestType.ResourceOwnerChange.ToString(), StringComparison.OrdinalIgnoreCase))
            .Where(req =>
                req.State != null &&
                req.State.Equals(RequestState.Created.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList()
            .Count;
    }

    public static int GetProjectChangesAffectingNextThreeMonths(
        IEnumerable<ApiChangeLogEvent> allRelevantEvents)
    {
        return allRelevantEvents
            .Where(ev => ev.ChangeType == ChangeType.PositionInstancePercentChanged.ToString()
                         || ev.ChangeType == ChangeType.PositionInstanceLocationChanged.ToString()
                         || ev.ChangeType == ChangeType.PositionInstanceAppliesFromChanged.ToString()
                         || ev.ChangeType == ChangeType.PositionInstanceAppliesToChanged.ToString())
            .ToList().Count;
    }

    public static IEnumerable<AllocatedPersonWithNoFutureAllocation> GetPersonnelPositionsEndingWithNoFutureAllocation(
        IEnumerable<IResourcesApiClient.InternalPersonnelPerson> listOfInternalPersonnel)
    {
        return listOfInternalPersonnel
            .Where(AllocatedPersonWithNoFutureAllocation.GotFutureAllocation)
            .Select(AllocatedPersonWithNoFutureAllocation.Create);
    }

    public static IEnumerable<AllocatedPersonnelWithWorkLoad> GetPersonnelAllocatedMoreThan100Percent(
        IEnumerable<IResourcesApiClient.InternalPersonnelPerson> listOfInternalPersonnel)
    {
        return listOfInternalPersonnel
            .Select(AllocatedPersonnelWithWorkLoad.Create)
            .Where(p => p.TotalWorkload > 100);
    }
}

public class AllocatedPersonnel
{
    public string FullName { get; }

    protected AllocatedPersonnel(IResourcesApiClient.InternalPersonnelPerson person)
    {
        FullName = person.Name;
    }
}

public class AllocatedPersonnelWithWorkLoad : AllocatedPersonnel
{
    public double TotalWorkload { get; }

    private AllocatedPersonnelWithWorkLoad(IResourcesApiClient.InternalPersonnelPerson person) : base(person)
    {
        TotalWorkload = CalculateTotalWorkload(person);
    }

    public static AllocatedPersonnelWithWorkLoad Create(IResourcesApiClient.InternalPersonnelPerson person)
    {
        return new AllocatedPersonnelWithWorkLoad(person);
    }

    private double CalculateTotalWorkload(IResourcesApiClient.InternalPersonnelPerson person)
    {
        var totalWorkLoad = person.EmploymentStatuses
            .Where(ab => ab.Type != IResourcesApiClient.ApiAbsenceType.Absence && ab.IsActive)
            .Select(ab => ab.AbsencePercentage).Sum();
        totalWorkLoad += person.PositionInstances.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum();
        if (totalWorkLoad is null)
            return 0;

        if (totalWorkLoad < 0)
            return 0;

        return totalWorkLoad.Value;
    }
}

public class AllocatedPersonWithNoFutureAllocation : AllocatedPersonnel
{
    public DateTime? EndDate { get; }


    private AllocatedPersonWithNoFutureAllocation(IResourcesApiClient.InternalPersonnelPerson person) : base(person)
    {
        var endingPosition = person.PositionInstances.Find(instance => instance.IsActive);
        if (endingPosition is null)
        {
            EndDate = null;
            return;
        }

        EndDate = endingPosition.AppliesTo;
    }

    public static AllocatedPersonWithNoFutureAllocation Create(IResourcesApiClient.InternalPersonnelPerson person)
    {
        return new AllocatedPersonWithNoFutureAllocation(person);
    }

    public static bool GotFutureAllocation(IResourcesApiClient.InternalPersonnelPerson person)
    {
        var gotLongLastingPosition = person.PositionInstances.Any(pdi => pdi.AppliesTo >= DateTime.UtcNow.AddMonths(3));
        if (gotLongLastingPosition)
            return false;

        var gotFutureAllocation = person.PositionInstances.Any(pdi => pdi.AppliesFrom > DateTime.UtcNow);
        if (gotFutureAllocation)
            return false;

        return person.PositionInstances.Any(pdi => pdi.IsActive);
    }
}

public enum RequestState
{
    Approval,
    Proposal,
    Provisioning,
    Created,
    Completed
}

public enum RequestType
{
    Allocation,
    ResourceOwnerChange
}

public enum ChangeType
{
    PositionInstanceCreated,
    PersonAssignedToPosition,
    PositionInstanceAllocationStateChanged,
    PositionInstanceAppliesToChanged,
    PositionInstanceAppliesFromChanged,
    PositionInstanceParentPositionIdChanged,
    PositionInstancePercentChanged,
    PositionInstanceLocationChanged,
}