using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Resources.Functions.Common.ApiClients;

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

    public static int GetAverageTimeToHandleRequests(
        IEnumerable<IResourcesApiClient.ResourceAllocationRequest> requests)
    {
        /*
         * Average time to handle request: average number of days from request created/sent to candidate is proposed - last 12 months
         * Calculation:
         * We find all the requests for the last 12 months that are not of type "ResourceOwnerChange" (for the specific Department)
         * For each of these request we find the number of days that it takes from when a requests is created (which is when the request is sent from Task Owner to ResourceOwner) 
         * to the requests is handeled by ResourceOwner (a person is proposed). If the request is still being processed it will not have a date for when
         * it is handled (proposed date), and then we will use todays date.
         * We then sum up the total amount of days used to handle a request and divide by the total number of requests for which we have found the handle-time
         */

        var requestsHandledByResourceOwner = 0;
        var totalNumberOfDays = 0.0;
        var twelveMonths = DateTime.UtcNow.AddMonths(-12);


        // Not to include requests that are sent by ResourceOwners (ResourceOwnerChange) or requests created more than 3 months ago
        var requestsLastTwelveMonthsWithoutResourceOwnerChangeRequest = requests
            .Where(req => req.Created > twelveMonths)
            .Where(r => r.Workflow is not null)
            .Where(_ => true)
            .Where(req => req.Type != null && !req.Type.Equals(RequestType.ResourceOwnerChange.ToString(),
                StringComparison.OrdinalIgnoreCase));

        foreach (var request in requestsLastTwelveMonthsWithoutResourceOwnerChangeRequest)
        {
            // If the requests doesnt have state it means that it is in draft. Do not need to check these
            if (request.State == null)
                continue;
            if (request.Workflow?.Steps is null)
                continue;

            // First: find the date for creation (this implies that the request has been sent to resourceowner)
            var dateForCreation = request.Workflow.Steps
                .FirstOrDefault(step => step.Name.Equals("Created") && step.IsCompleted)?.Completed.Value.DateTime;

            if (dateForCreation == null)
                continue;

            //Second: Try to find the date for proposed (this implies that resourceowner have handled the request)
            var dateOfApprovalOrToday = request.Workflow.Steps
                .FirstOrDefault(step => step.Name.Equals("Proposed") && step.IsCompleted)?.Completed.Value.DateTime;

            // if there are no proposal date we will used todays date for calculation 
            dateOfApprovalOrToday = dateOfApprovalOrToday ?? DateTime.UtcNow;


            requestsHandledByResourceOwner++;
            var timespanDifference = dateOfApprovalOrToday - dateForCreation;
            var differenceInDays = timespanDifference.Value.TotalDays;
            totalNumberOfDays += differenceInDays;
        }

        if (requestsHandledByResourceOwner <= 0)
            return 0;

        var averageAmountOfTimeDouble = totalNumberOfDays / requestsHandledByResourceOwner;
        return (int)Math.Round(averageAmountOfTimeDouble);
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

    public static int CalculateDepartmentChangesLastWeek(IEnumerable<IResourcesApiClient.InternalPersonnelPerson> internalPersonnel)
    {
        /* 
         * How we calculate the changes:
         * Find all active instanses or all instanses for each personnel that starts within 3 months
         * To find the instances that have changes related to them:
         * Find all instances that have the field "AllocationState" not set to null
         * Find all instances where AllocationUpdated > 7 days ago
        */

        var threeMonthsFromToday = DateTime.UtcNow.AddMonths(3);
        var today = DateTime.UtcNow;
        var weekBackInTime = DateTime.UtcNow.AddDays(-7);

        // Find all active (IsActive) instances or instances that have start date (appliesFrom) > threeMonthsFromToday
        var instancesThatAreActiveOrBecomesActiveWithinThreeMonths = internalPersonnel
            .SelectMany(per => per.PositionInstances
            .Where(pis => (pis.AppliesFrom < threeMonthsFromToday && pis.AppliesFrom > today) || pis.AppliesTo > today || pis.IsActive));

        var instancesWithAllocationStateSetAndAllocationUpdateWithinLastWeek = instancesThatAreActiveOrBecomesActiveWithinThreeMonths
            .Where(per => per.AllocationState != null)
            .Where(pos => pos.AllocationUpdated != null && pos.AllocationUpdated > weekBackInTime).ToList();

        return instancesWithAllocationStateSetAndAllocationUpdateWithinLastWeek.Count();
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