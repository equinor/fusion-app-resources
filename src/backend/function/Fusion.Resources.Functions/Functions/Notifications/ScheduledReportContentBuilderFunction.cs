using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients.ApiModels;
using Fusion.Resources.Functions.Functions.Notifications.Models;
using Fusion.Resources.Functions.Functions.Notifications.Models.AdaptiveCards;
using Fusion.Resources.Functions.Functions.Notifications.Models.DTOs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Fusion.Resources.Functions.Functions.Notifications.Models.AdaptiveCards.AdaptiveCardBuilder;
using Fusion.Resources.Functions.ApiClients;
using static Fusion.Resources.Functions.ApiClients.IResourcesApiClient;
using Fusion.Integration;
using Fusion.Integration.Configuration;
using Fusion.Integration.ServiceDiscovery;
using Microsoft.Extensions.Configuration;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportContentBuilderFunction
{
    private readonly ILogger<ScheduledReportContentBuilderFunction> _logger;
    private readonly INotificationApiClient _notificationsClient;
    private readonly IResourcesApiClient _resourceClient;
    private readonly IOrgClient _orgClient;
    private const string FormatDoubleToHaveOneDecimal = "F1";
    private readonly IConfiguration _configuration;

    public ScheduledReportContentBuilderFunction(ILogger<ScheduledReportContentBuilderFunction> logger,
        IResourcesApiClient resourcesApiClient,
        INotificationApiClient notificationsClient,
        IOrgClient orgClient, IConfiguration configuration)
    {
        _logger = logger;
        _resourceClient = resourcesApiClient;
        _notificationsClient = notificationsClient;
        _orgClient = orgClient;
        _configuration = configuration;
    }

    [FunctionName("scheduled-report-content-Builder-function")]
    public async Task RunAsync(
        [ServiceBusTrigger("%scheduled_notification_report_queue%", Connection = "AzureWebJobsServiceBus")]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageReceiver)
    {
        _logger.LogInformation(
            $"{nameof(ScheduledReportContentBuilderFunction)} " +
            $"started with message: {message.Body}");
        try
        {
            var body = Encoding.UTF8.GetString(message.Body);
            var dto = JsonConvert.DeserializeObject<ScheduledNotificationQueueDto>(body);
            if (!Guid.TryParse(dto.AzureUniqueId, out var azureUniqueId))
                throw new Exception("AzureUniqueId not valid.");
            if (string.IsNullOrEmpty(dto.FullDepartment))
                throw new Exception("FullDepartmentIdentifier not valid.");


            switch (dto.Role)
            {
                case NotificationRoleType.ResourceOwner:
                    await BuildContentForResourceOwner(azureUniqueId, dto.FullDepartment, dto.DepartmentSapId);
                    break;
                case NotificationRoleType.TaskOwner:
                    await BuildContentForTaskOwner(azureUniqueId);
                    break;
                default:
                    throw new Exception("Role not valid.");
            }

            _logger.LogInformation(
                $"{nameof(ScheduledReportContentBuilderFunction)} " +
                $"finished with message: {message.Body}");
        }
        catch (Exception e)
        {
            _logger.LogError(
                $"{nameof(ScheduledReportContentBuilderFunction)} " +
                $"failed with exception: {e.Message}");
        }
        finally
        {
            // Complete the message regardless of outcome.
            await messageReceiver.CompleteMessageAsync(message);
        }
    }

    private async Task BuildContentForTaskOwner(Guid azureUniqueId)
    {
        throw new NotImplementedException();
    }

    private async Task BuildContentForResourceOwner(Guid azureUniqueId, string fullDepartment, string departmentSapId)
    {
        var threeMonthsFuture = DateTime.UtcNow.AddMonths(3);
        var today = DateTime.UtcNow;
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // Get all requests for department regardsless of state
        var departmentRequests = await _resourceClient.GetAllRequestsForDepartment(fullDepartment);
        // Get all the personnel for the department
        var personnelForDepartment = await _resourceClient.GetAllPersonnelForDepartment(fullDepartment);
        // Adding leave
        personnelForDepartment = await GetPersonnelLeave(personnelForDepartment);


        //1.Number of personnel
        var numberOfPersonnel = personnelForDepartment.Count();

        //2.Capacity in use:
        var percentageOfTotalCapacity = FindTotalCapacityIncludingLeave(personnelForDepartment.ToList());

        // 3.New requests last week (7 days)
        var numberOfRequestsLastWeek = departmentRequests.Count(req =>
            req.Type != null && !req.Type.Equals("ResourceOwnerChange") && req.Created > sevenDaysAgo && !req.IsDraft);

        //4.Open request (no proposedPerson)
        var totalNumberOfOpenRequests = departmentRequests.Count(req =>
            req.State != null && req.Type != null && !req.Type.Equals("ResourceOwnerChange") &&
            !req.HasProposedPerson &&
            !req.State.Equals("completed", StringComparison.OrdinalIgnoreCase));


        //5.Requests with start-date < 3 months
        var numberOfDepartmentRequestWithLessThanThreeMonthsBeforeStartAndNoNomination = departmentRequests
            .Count(x => x.OrgPositionInstance != null &&
                        x.State != null &&
                        !x.State.Equals("completed") &&
                        (x.OrgPositionInstance.AppliesFrom < threeMonthsFuture &&
                         x.OrgPositionInstance.AppliesFrom > today) && !x.HasProposedPerson);


        //6.Requests with start-date > 3 months:
        var numberOfDepartmentRequestWithMoreThanThreeMonthsBeforeStart = departmentRequests
            .Count(x => x.OrgPositionInstance != null &&
                        x.State != null &&
                        !x.State.Contains("completed") &&
                        x.OrgPositionInstance.AppliesFrom > threeMonthsFuture);

        //7.Average time to handle request (last 3 months): 
        var averageTimeToHandleRequest = CalculateAverageTimeToHandleRequests(departmentRequests);

        //8.Allocation changes awaiting task owner action:
        //number of allocation changes made by resource owner awaiting task owner action
        //M� hente ut alle posisjoner som har ressurser for en gitt avdeling og sjekke p� om det er gjort endringer her den siste tiden
        var numberOfAllocationchangesAwaitingTaskOwnerAction = GetchangesAwaitingTaskOwnerAction(departmentRequests);

        //9.Project changes affecting next 3 months
        //number of project changes(changes initiated by project / task) with a change affecting the next 3 months
        var numberOfChangesAffectingNextThreeMonths = GetAllChangesForResourceDepartment(personnelForDepartment);

        //10.Allocations ending soon with no future allocation
        var listOfPersonnelWithoutFutureAllocations = FilterPersonnelWithoutFutureAllocations(personnelForDepartment);

        //11.Personnel with more than 100 % workload
        var listOfPersonnelsWithMoreThan100Percent = personnelForDepartment.Where(p =>
            p.PositionInstances.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum() > 100);
        var listOfPersonnelForDepartmentWithMoreThan100Percent =
            listOfPersonnelsWithMoreThan100Percent.Select(p => CreatePersonnelWithTBEContent(p));


        var card = ResourceOwnerAdaptiveCardBuilder(new ResourceOwnerAdaptiveCardData
            {
                TotalNumberOfPersonnel = numberOfPersonnel,
                TotalCapacityInUsePercentage = percentageOfTotalCapacity,
                NumberOfRequestsLastWeek = numberOfRequestsLastWeek,
                NumberOfOpenRequests = totalNumberOfOpenRequests,
                NumberOfRequestsStartingInMoreThanThreeMonths =
                    numberOfDepartmentRequestWithMoreThanThreeMonthsBeforeStart,
                NumberOfRequestsStartingInLessThanThreeMonths =
                    numberOfDepartmentRequestWithLessThanThreeMonthsBeforeStartAndNoNomination,
                AverageTimeToHandleRequests = averageTimeToHandleRequest,
                AllocationChangesAwaitingTaskOwnerAction = numberOfAllocationchangesAwaitingTaskOwnerAction,
                ProjectChangesAffectingNextThreeMonths = numberOfChangesAffectingNextThreeMonths,
                PersonnelPositionsEndingWithNoFutureAllocation = listOfPersonnelWithoutFutureAllocations,
                PersonnelAllocatedMoreThan100Percent = listOfPersonnelForDepartmentWithMoreThan100Percent
            },
            fullDepartment, departmentSapId);

        var sendNotification = await _notificationsClient.SendNotification(
            new SendNotificationsRequest()
            {
                Title = $"Weekly summary - {fullDepartment}",
                EmailPriority = 1,
                Card = card.Result,
                Description = $"Weekly report for department - {fullDepartment}"
            },
            azureUniqueId);

        // Throwing exception if the response is not successful.
        if (!sendNotification)
        {
            throw new Exception(
                $"Failed to send notification to resource-owner with AzureUniqueId: '{azureUniqueId}'.");
        }
    }

    private IEnumerable<PersonnelContent> FilterPersonnelWithoutFutureAllocations(
        IEnumerable<InternalPersonnelPerson> personnelForDepartment)
    {
        var threeMonthsFuture = DateTime.UtcNow.AddMonths(3);

        var personnelWithPositionsEndingInThreeMonths = personnelForDepartment.Where(x =>
            x.PositionInstances.Where(pos => pos.IsActive && pos.AppliesTo <= threeMonthsFuture).Any());
        var personnelWithoutFutureAllocations = personnelWithPositionsEndingInThreeMonths.Where(person =>
            person.PositionInstances.All(pos => pos.AppliesTo < threeMonthsFuture));
        return personnelWithoutFutureAllocations.Select(p => CreatePersonnelContent(p));
    }

    private async Task<IEnumerable<InternalPersonnelPerson>> GetPersonnelLeave(
        IEnumerable<InternalPersonnelPerson> listOfInternalPersonnel)
    {
        List<InternalPersonnelPerson> newList = listOfInternalPersonnel.ToList();
        for (int i = 0; i < newList.Count(); i++)
        {
            var absence = await _resourceClient.GetLeaveForPersonnel(newList[i].AzureUniquePersonId.ToString());
            newList[i].ApiPersonAbsences = absence.ToList();
        }

        listOfInternalPersonnel = newList;
        return listOfInternalPersonnel;
    }

    private int FindTotalCapacityIncludingLeave(List<InternalPersonnelPerson> listOfInternalPersonnel)
    {
        //Calculated by total current workload for all personnel / (100 % workload x number of personnel - (total % leave)), 
        //a.e.g. 10 people in department: 800 % current workload / (1000 % -120 % leave) = 91 % capacity in use
        // We need to take into account the other types of allocations from absence-endpoint.

        var totalWorkLoad = 0.0;
        double? totalLeave = 0.0;

        foreach (var personnel in listOfInternalPersonnel)
        {
            totalLeave += personnel.ApiPersonAbsences.Where(ab => ab.Type == ApiAbsenceType.Absence && ab.IsActive)
                .Select(ab => ab.AbsencePercentage).Sum();
            totalWorkLoad += (double)personnel.ApiPersonAbsences
                .Where(ab => ab.Type != ApiAbsenceType.Absence && ab.IsActive).Select(ab => ab.AbsencePercentage).Sum();
            totalWorkLoad += personnel.PositionInstances.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum();
        }

        var totalPercentageInludeLeave = totalWorkLoad / ((listOfInternalPersonnel.Count * 100) - totalLeave) * 100;

        return Convert.ToInt32(totalPercentageInludeLeave);
    }

    private int GetAllChangesForResourceDepartment(IEnumerable<InternalPersonnelPerson> listOfInternalPersonnel)
    {
        // Find all active instances (we get projectId, positionId and instanceId from this)
        // Then check if the changes are changes in split (duration, workload, location) - TODO: Check if there are other changes that should be accounted for

        var threeMonths = DateTime.UtcNow.AddMonths(3);
        var today = DateTime.UtcNow;

        var listOfInternalPersonnelwithOnlyActiveProjects = listOfInternalPersonnel.SelectMany(per =>
            per.PositionInstances.Where(pis =>
                pis.IsActive || (pis.AppliesFrom < threeMonths && pis.AppliesFrom > today)));

        int totalChangesForDepartment = 0;

        foreach (var instance in listOfInternalPersonnelwithOnlyActiveProjects)
        {
            if (instance.Project == null)
                continue;

            var changeLogForPersonnel = _orgClient.GetChangeLog(instance.Project.Id.ToString(),
                instance.PositionId.ToString(), instance.InstanceId.ToString());
            var totalChanges = changeLogForPersonnel.Result.Events
                .Where(ev => ev.Instance != null
                             && ev.ChangeType != ChangeType.PositionInstanceCreated
                             && ev.ChangeType != ChangeType.PersonAssignedToPosition
                             && ev.ChangeType != ChangeType.PositionInstanceAllocationStateChanged
                             && ev.ChangeType != ChangeType.PositionInstanceParentPositionIdChanged
                             && (ev.ChangeType.Equals(ChangeType.PositionInstanceAppliesToChanged) &&
                                 ev.Instance.AppliesTo < threeMonths));


            totalChangesForDepartment += totalChanges.Count();
        }

        return totalChangesForDepartment;
    }

    private int GetchangesAwaitingTaskOwnerAction(IEnumerable<ResourceAllocationRequest> listOfRequests)
        => listOfRequests.Where((req => req.Type is "ResourceOwnerChange")).Where(req =>
                req.Workflow != null && req.Workflow.Steps.Any(step => step.Name.Equals("Accept") && !step.IsCompleted))
            .ToList().Count();


    private double CalculateAverageTimeToHandleRequests(IEnumerable<ResourceAllocationRequest> listOfRequests)
    {
        /* How to calculate:
         *
         * Find the workflow "created" and then find the date
         * This should mean that task owner have created and sent the request to resource owner
         * Find the workflow "proposal" and then find the date
         * This should mean that the resource owner have done their bit
         * TODO: Maybe we need to consider other states
         */

        var requestsHandledByResourceOwner = 0;
        var totalNumberOfDays = 0.0;

        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

        var requestsLastThreeMonthsWithoutResourceOwnerChangeRequest = listOfRequests
            .Where(req => req.Created > threeMonthsAgo)
            .Where(r => r.Workflow is not null)
            .Where(_ => true)
            .Where((req => req.Type != null && !req.Type.Equals("ResourceOwnerChange")));

        foreach (var request in requestsLastThreeMonthsWithoutResourceOwnerChangeRequest)
        {
            if (request.State is "created")
                continue;

            var dateForCreation = request.Workflow.Steps
                .FirstOrDefault(step => step.Name.Equals("Created") && step.IsCompleted)?.Completed.Value.DateTime;
            var dateForApproval = request.Workflow.Steps
                .FirstOrDefault(step => step.Name.Equals("Proposed") && step.IsCompleted)?.Completed.Value.DateTime;
            if (dateForCreation != null && dateForApproval != null)
            {
                requestsHandledByResourceOwner++;
                var timespanDifference = dateForApproval - dateForCreation;
                var differenceInDays = timespanDifference.Value.TotalDays;
                totalNumberOfDays += differenceInDays;
            }
        }

        if (!(totalNumberOfDays > 0))
            return 0;

        return totalNumberOfDays / requestsHandledByResourceOwner;
    }

    private PersonnelContent CreatePersonnelContent(InternalPersonnelPerson person)
    {
        if (person == null)
            throw new ArgumentNullException();

        var position = person.PositionInstances.Find(instance => instance.IsActive);
        var positionName = position.Name;
        var projectName = position.Project.Name;
        var personnelContent = new PersonnelContent()
        {
            FullName = person.Name,
            PositionName = positionName,
            ProjectName = projectName,
            EndingPosition = position
        };
        return personnelContent;
    }

    private PersonnelContent CreatePersonnelWithTBEContent(InternalPersonnelPerson person)
    {
        var positionInstances = person.PositionInstances.Where(pos => pos.IsActive);
        var sumWorkload = positionInstances.Select(pos => pos.Workload).Sum();
        var numberOfPositionInstances = positionInstances.Count();
        var personnelContent = new PersonnelContent()
        {
            FullName = person.Name,
            TotalWorkload = sumWorkload,
            NumberOfPositionInstances = numberOfPositionInstances,
        };
        return personnelContent;
    }

    private async Task<AdaptiveCard> ResourceOwnerAdaptiveCardBuilder(ResourceOwnerAdaptiveCardData cardData,
        string departmentIdentifier, string departmentSapId)
    {
        var personnelAllocationUri = $"{PortalUri()}apps/personnel-allocation/{departmentSapId}";
        var card = new AdaptiveCardBuilder()
            .AddHeading($"**Weekly summary - {departmentIdentifier}**")
            .AddColumnSet(new AdaptiveCardColumn(cardData.TotalNumberOfPersonnel.ToString(), "Number of personnel"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.TotalCapacityInUsePercentage.ToString(), "Capacity in use",
                "%"))
            .AddColumnSet(
                new AdaptiveCardColumn(cardData.NumberOfRequestsLastWeek.ToString(), "New requests last week"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.NumberOfOpenRequests.ToString(), "Open requests"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.NumberOfRequestsStartingInLessThanThreeMonths.ToString(),
                "Requests with start date < 3 months"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.NumberOfRequestsStartingInMoreThanThreeMonths.ToString(),
                "Requests with start date > 3 months"))
            .AddColumnSet(new AdaptiveCardColumn(
                cardData.AverageTimeToHandleRequests.ToString(FormatDoubleToHaveOneDecimal),
                "Average time to handle request", "days"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.AllocationChangesAwaitingTaskOwnerAction.ToString(),
                "Allocation changes awaiting task owner action"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.ProjectChangesAffectingNextThreeMonths.ToString(),
                "Project changes affecting next 3 months"))
            .AddListContainer("Allocations ending soon with no future allocation:",
                cardData.PersonnelPositionsEndingWithNoFutureAllocation, "FullName", "EndingPosition")
            .AddListContainer("Personnel with more than 100% workload:", cardData.PersonnelAllocatedMoreThan100Percent,
                "FullName", "TotalWorkload")
            .AddActionButton("Go to Personnel allocation app", personnelAllocationUri)
            .Build();

        return card;
    }

    private string PortalUri()
    {
        var portalUri = _configuration["Endpoints_portal"] ?? "https://fusion.equinor.com/";
        if (!portalUri.EndsWith("/"))
            portalUri += "/";
        return portalUri;
    }
}