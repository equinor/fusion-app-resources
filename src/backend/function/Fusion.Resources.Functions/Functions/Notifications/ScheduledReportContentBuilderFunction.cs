using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.ApiClients.ApiModels;
using Fusion.Resources.Functions.Functions.Notifications.Models;
using Fusion.Resources.Functions.Functions.Notifications.Models.AdaptiveCards;
using Fusion.Resources.Functions.Functions.Notifications.Models.DTOs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Fusion.Resources.Functions.ApiClients.IResourcesApiClient;
using static Fusion.Resources.Functions.Functions.Notifications.Models.AdaptiveCards.AdaptiveCardBuilder;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportContentBuilderFunction
{
    private readonly ILogger<ScheduledReportContentBuilderFunction> _logger;
    private readonly INotificationApiClient _notificationsClient;
    private readonly IResourcesApiClient _resourceClient;

    public ScheduledReportContentBuilderFunction(ILogger<ScheduledReportContentBuilderFunction> logger,
        IResourcesApiClient resourcesApiClient,
        INotificationApiClient notificationsClient)
    {
        _logger = logger;
        _resourceClient = resourcesApiClient;
        _notificationsClient = notificationsClient;
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
                    await BuildContentForResourceOwner(azureUniqueId, dto.FullDepartment);
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

    private async Task BuildContentForResourceOwner(Guid azureUniqueId, string fullDepartment)
    {
        var threeMonthsFuture = DateTime.UtcNow.AddMonths(3);
        var today = DateTime.UtcNow;
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // Get all requests for specific Department regardsless of state
        var departmentRequests = await _resourceClient.GetAllRequestsForDepartment(fullDepartment);

        // Get all the personnel for the specific department
        var personnelForDepartment = await _resourceClient.GetAllPersonnelForDepartment(fullDepartment);
        personnelForDepartment = await GetPersonnelLeave(personnelForDepartment);
        


        //1.Number of personnel: 
        //number of personnel in the department(one specific org unit)
        // Hvordan finne?
        // Hent ut alle ressursene for en gitt avdeling
        // OK?
        var numberOfPersonnel = personnelForDepartment.Count();

        //2.Capacity in use:
        //capacity in use by %.
        //Calculated by total current workload for all personnel / (100 % workload x number of personnel - (total % leave)), 
        //a.e.g. 10 people in department: 800 % current workload / (1000 % -120 % leave) = 91 % capacity in use
        // OK - Inkluderer nå leave..
        var percentageOfTotalCapacity = FindTotalCapacityIncludingLeave(personnelForDepartment.ToList());


        // 3.New requests last week:
        // number of requests received last 7 days
        // Notat: En request kan ha blitt opprettet for 7 dager siden, men ikke oversendt til ressurseiere - Det kan være 
        // Inkluderer foreløpig alle requestene uavhengig av hvilken state de er
        var numberOfRequestsLastWeek = departmentRequests.Where(req => req.Created > sevenDaysAgo && !req.IsDraft).Count();



        //4.Open request:
        //number of requests with no proposed candidate
        // Only to include those requests which have state approval (this means that the resource owner needs to process the requests in some way)
        // OK? - Må sjekkes og finne noen som har approval...
        var totalNumberOfOpenRequests = departmentRequests.Count(req => !req.HasProposedPerson && !req.State.Contains(RequestState.completed.ToString()));


        //5.Requests with start-date < 3 months:
        //number of requests with start date within less than 3 months
        // Filter to only inlclude the ones that have start-date in less than 3 months and start-date after today and is not complete and has no proposedPerson assigned to them
        var numberOfDepartmentRequestWithLessThanThreeMonthsBeforeStartAndNoNomination = departmentRequests
            .Count(x => !x.State.Contains(RequestState.completed.ToString()) &&
                (x.OrgPositionInstance.AppliesFrom < threeMonthsFuture &&
                 x.OrgPositionInstance.AppliesFrom > today) && !x.HasProposedPerson);


        //6.Requests with start-date > 3 months:
        //number of requests with start date later than next 3 months
        // Filter to only include the ones that have start-date in more than 3 months AND state not completed
        var numberOfDepartmentRequestWithMoreThanThreeMonthsBeforeStart = departmentRequests
            .Count(x => !x.State.Contains(RequestState.completed.ToString()) &&
                x.OrgPositionInstance.AppliesFrom > threeMonthsFuture);

        // TODO:
        //7.Average time to handle request: 
        //average number of days from request created/ sent to candidate is proposed - last 6 months

        // TODO:
        //8.Allocation changes awaiting task owner action:
        //number of allocation changes made by resource owner awaiting task owner action
        //Må hente ut alle posisjoner som har ressurser for en gitt avdeling og sjekke på om det er gjort endringer her den siste tiden

        // TODO: 
        //9.Project changes affecting next 3 months: 
        //number of project changes(changes initiated by project / task) with a change affecting the next 3 months

        //10.Allocations ending soon with no future allocation:  -Skal være ok ?
        //list of allocations ending within next 3 months where the person allocated does not continue in the position(i.e.no future splits with the same person allocated)
        var listOfPersonnelWithoutFutureAllocations = FilterPersonnelWithoutFutureAllocations(personnelForDepartment);

        //11.Personnel with more than 100 % workload: -OK
        //(as in current pilot, but remove "FTE") list of persons with total allocation > 100 %, total % workload should be visible after person name
        // TODO: Fiks formatering og oppdeling av innhold her
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
            NumberOfRequestsStartingInMoreThanThreeMonths = numberOfDepartmentRequestWithMoreThanThreeMonthsBeforeStart,
            NumberOfRequestsStartingInLessThanThreeMonths = numberOfDepartmentRequestWithLessThanThreeMonthsBeforeStartAndNoNomination,
            AverageTimeToHandleRequests = 0,
            AllocationChangesAwaitingTaskOwnerAction = 0,
            ProjectChangesAffectingNextThreeMonths = 0,
            PersonnelPositionsEndingWithNoFutureAllocation = listOfPersonnelWithoutFutureAllocations,
            PersonnelAllocatedMoreThan100Percent = listOfPersonnelForDepartmentWithMoreThan100Percent
        },
            fullDepartment);

        var sendNotification = await _notificationsClient.SendNotification(
            new SendNotificationsRequest()
            {
                Title = $"Weekly summary - {fullDepartment}",
                EmailPriority = 1,
                Card = card,
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

    private async Task<IEnumerable<InternalPersonnelPerson>> GetPersonnelLeave(IEnumerable<InternalPersonnelPerson> listOfInternalPersonnel)
    {
        List<InternalPersonnelPerson> newList = listOfInternalPersonnel.ToList();
        for (int i =0; i < newList.Count(); i++)
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

        var totalWorkLoad = 0.0;
        double? totalLeave = 0.0;

        foreach (var personnel in listOfInternalPersonnel)
        {
            totalLeave += personnel.ApiPersonAbsences.Where(ab => ab.Type == ApiAbsenceType.Absence && ab.IsActive).Select(ab => ab.AbsencePercentage).Sum();
            totalWorkLoad += personnel.PositionInstances.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum();
        }

        var totalPercentageInludeLeave = totalWorkLoad / ((listOfInternalPersonnel.Count * 100) - totalLeave) * 100;

        return Convert.ToInt32(totalPercentageInludeLeave);
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

    private static AdaptiveCard ResourceOwnerAdaptiveCardBuilder(ResourceOwnerAdaptiveCardData cardData,
        string departmentIdentifier)
    {
        // Plasser denne en annen plass
        var baseUri = "https://fusion-s-portal-ci.azurewebsites.net/apps/personnel-allocation/";
        var avdelingsId = "52586050";


        var card = new AdaptiveCardBuilder()
        .AddHeading($"**Weekly summary - {departmentIdentifier}**")
        .AddColumnSet(new AdaptiveCardColumn(cardData.TotalNumberOfPersonnel.ToString(), "Number of personnel"))
        .AddColumnSet(new AdaptiveCardColumn(cardData.TotalCapacityInUsePercentage.ToString(), "Capacity in use", "%"))
        .AddColumnSet(new AdaptiveCardColumn(cardData.NumberOfRequestsLastWeek.ToString(), "New requests last week"))
        .AddColumnSet(new AdaptiveCardColumn(cardData.NumberOfOpenRequests.ToString(), "Open requests"))
        .AddColumnSet(new AdaptiveCardColumn(cardData.NumberOfRequestsStartingInLessThanThreeMonths.ToString(), "Requests with start date < 3 months"))
        .AddColumnSet(new AdaptiveCardColumn(cardData.NumberOfRequestsStartingInMoreThanThreeMonths.ToString(), "Requests with start date > 3 months"))

        // Not finished
        .AddColumnSet(new AdaptiveCardColumn(/*(cardData.AverageTimeToHandleRequests.ToString()*/ "NA", "Average time to handle request")) // WIP
        .AddColumnSet(new AdaptiveCardColumn(/*cardData.AllocationChangesAwaitingTaskOwnerAction.ToString()*/ "NA", "Allocation changes awaiting task owner action")) // WIP
        .AddColumnSet(new AdaptiveCardColumn(/*cardData.ProjectChangesAffectingNextThreeMonths.ToString()*/ "NA", "Project changes affecting next 3 months")) // WIP

        .AddListContainer("Positions ending soon with no future allocation:", cardData.PersonnelPositionsEndingWithNoFutureAllocation, "FullName", "EndingPosition")
        .AddListContainer("Personnel with more than 100% workload:", cardData.PersonnelAllocatedMoreThan100Percent, "FullName", "TotalWorkload")

        .AddActionButton("Go to Personnel allocation app", $"{baseUri}{avdelingsId}")

        .Build();

        return card;
    }

}