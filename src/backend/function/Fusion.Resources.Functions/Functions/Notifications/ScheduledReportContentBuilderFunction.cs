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
using static Fusion.Resources.Functions.Functions.Notifications.AdaptiveCardBuilder;
using Fusion.Resources.Functions.ApiClients;
using static Fusion.Resources.Functions.ApiClients.IResourcesApiClient;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportContentBuilderFunction
{
    private readonly ILogger<ScheduledReportContentBuilderFunction> _logger;
    private readonly INotificationApiClient _notificationsClient;
    private readonly IResourcesApiClient _resourceClient;
    private readonly IOrgClient _orgClient;
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
        var threeMonthsFromToday = DateTime.UtcNow.AddMonths(3);
        var today = DateTime.UtcNow;

        //  Requests for department
        var departmentRequests = await _resourceClient.GetAllRequestsForDepartment(fullDepartment);
        // Personnel for the department
        var personnelForDepartment = await GetPersonnelWithLeave(fullDepartment);
        // Capacity in use
        var capacityInUse = CapacityInUse(personnelForDepartment);

        // New requests last week (7 days)
        var numberOfRequestsLastWeek = departmentRequests
            .Count(req => req.Type != null && !req.Type.Equals("ResourceOwnerChange")
                                           && req.Created > DateTime.UtcNow.AddDays(-7) && !req.IsDraft);

        // Open request (no proposedPerson)
        var totalNumberOfOpenRequests = departmentRequests.Count(req =>
            req.State != null && req.Type != null && !req.Type.Equals("ResourceOwnerChange") &&
            !req.HasProposedPerson &&
            !req.State.Equals("completed", StringComparison.OrdinalIgnoreCase));


        // Requests with start-date greater than three months form today
        var numberOfDepartmentRequestWithLessThanThreeMonthsBeforeStartAndNoNomination = departmentRequests
            .Count(x => x.OrgPositionInstance != null &&
                        x.State != null &&
                        !x.State.Equals("completed") && !x.Type.Equals("ResourceOwnerChange") &&
                        (x.OrgPositionInstance.AppliesFrom < threeMonthsFromToday &&
                         x.OrgPositionInstance.AppliesFrom > today) && !x.HasProposedPerson);


        // Requests with start-date less than three months form today
        var numberOfDepartmentRequestWithMoreThanThreeMonthsBeforeStart = departmentRequests
            .Count(x => x.Type != null &&
                        x.OrgPositionInstance != null &&
                        x.State != null &&
                        !x.State.Contains("completed") && !x.Type.Equals("ResourceOwnerChange") &&
                        x.OrgPositionInstance.AppliesFrom > threeMonthsFromToday);

        // Average time to handle request (last 3 months)
        var averageTimeToHandleRequest = CalculateAverageTimeToHandleRequests(departmentRequests);

        // Allocation changes awaiting task owner action
        var numberOfAllocationChangesAwaitingTaskOwnerAction = GetChangesAwaitingTaskOwnerAction(departmentRequests);

        // Project changes affecting next 3 months
        var numberOfChangesAffectingNextThreeMonths = await GetAllChangesForResourceDepartment(personnelForDepartment);

        // Allocations ending soon with no future allocation
        var listOfPersonnelWithoutFutureAllocations = FilterPersonnelWithoutFutureAllocations(personnelForDepartment);

        var listOfPersonnelWithTbeContent = personnelForDepartment
            .Select(CreatePersonnelContentWithTotalWorkload);
        var personnelAllocatedMoreThan100Percent = listOfPersonnelWithTbeContent
            .Where(p => p.TotalWorkload > 100);

        var card = ResourceOwnerAdaptiveCardBuilder(new ResourceOwnerAdaptiveCardData
        {
            TotalNumberOfPersonnel = personnelForDepartment.Count(),
            CapacityInUse = capacityInUse,
            NumberOfRequestsLastWeek = numberOfRequestsLastWeek,
            NumberOfOpenRequests = totalNumberOfOpenRequests,
            NumberOfRequestsStartingInMoreThanThreeMonths =
                    numberOfDepartmentRequestWithMoreThanThreeMonthsBeforeStart,
            NumberOfRequestsStartingInLessThanThreeMonths =
                    numberOfDepartmentRequestWithLessThanThreeMonthsBeforeStartAndNoNomination,
            AverageTimeToHandleRequests = averageTimeToHandleRequest,
            AllocationChangesAwaitingTaskOwnerAction = numberOfAllocationChangesAwaitingTaskOwnerAction,
            ProjectChangesAffectingNextThreeMonths = numberOfChangesAffectingNextThreeMonths,
            PersonnelPositionsEndingWithNoFutureAllocation = listOfPersonnelWithoutFutureAllocations,
            PersonnelAllocatedMoreThan100Percent = personnelAllocatedMoreThan100Percent
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
        var pdList = new List<InternalPersonnelPerson>();
        foreach (var pd in personnelForDepartment)
        {
            var gotLongLastingPosition = pd.PositionInstances.Any(pdi => pdi.AppliesTo >= DateTime.UtcNow.AddMonths(3));
            if (gotLongLastingPosition)
                continue;

            var gotFutureAllocation = pd.PositionInstances.Any(pdi => pdi.AppliesFrom > DateTime.UtcNow);
            if (gotFutureAllocation)
                continue;
            var gotActiveAllocation = pd.PositionInstances.Any(pdi => pdi.IsActive);
            if (!gotActiveAllocation)
                continue;

            pdList.Add(pd);
        }

        return pdList.Select(CreatePersonnelContentWithPositionInformation);
    }


    private async Task<IEnumerable<InternalPersonnelPerson>> GetPersonnelWithLeave(string fullDepartment)
    {
        var personnel = await _resourceClient.GetAllPersonnelForDepartment(fullDepartment);
        if (!personnel.Any())
            return new List<InternalPersonnelPerson>();

        var tasks = personnel.Select(async person =>
        {
            var absence = await _resourceClient.GetLeaveForPersonnel(person.AzureUniquePersonId.ToString());
            person.ApiPersonAbsences = absence.ToList();
            return person;
        });

        var results = await Task.WhenAll(tasks);

        return results;
    }

    private static int CapacityInUse(IEnumerable<InternalPersonnelPerson> listOfInternalPersonnel)
    {
        var actualWorkLoad = 0.0;
        var actualLeave = 0.0;
        foreach (var personnel in listOfInternalPersonnel)
        {
            actualWorkLoad += personnel.PositionInstances.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum();
            actualWorkLoad += personnel.ApiPersonAbsences
                .Where(ab => ab.Type == ApiAbsenceType.OtherTasks && ab.IsActive)
                .Select(ab => ab.AbsencePercentage)
                .Sum() ?? 0;
            actualLeave += personnel.ApiPersonAbsences
                .Where(ab => (ab.Type == ApiAbsenceType.Absence || ab.Type == ApiAbsenceType.Vacation) && ab.IsActive)
                .Select(ab => ab.AbsencePercentage)
                .Sum() ?? 0;
        }

        var maximumPotentialWorkLoad = listOfInternalPersonnel.Count() * 100;
        var potentialWorkLoad = maximumPotentialWorkLoad - actualLeave;
        if (potentialWorkLoad <= 0)
            return 0;
        var capacityInUse = actualWorkLoad / potentialWorkLoad * 100;
        if (capacityInUse < 0)
            return 0;

        return (int)Math.Round(capacityInUse);
    }

    private async Task<int> GetAllChangesForResourceDepartment(
        IEnumerable<InternalPersonnelPerson> listOfInternalPersonnel)
    {
        // Find all active instances (we get projectId, positionId and instanceId from this)
        // Then check if the changes are changes in split (duration, workload, location) - TODO: Check if there are other changes that should be accounted for

        var threeMonthsFromToday = DateTime.UtcNow.AddMonths(3);
        var today = DateTime.UtcNow;

        var listOfInternalPersonnelwithOnlyActiveProjects = listOfInternalPersonnel.SelectMany(per =>
            per.PositionInstances.Where(pis =>
                (pis.AppliesFrom < threeMonthsFromToday && pis.AppliesFrom > today) || pis.AppliesTo > today));

        var totalChangesForDepartment = 0;

        var tasks = listOfInternalPersonnelwithOnlyActiveProjects
            .Where(pl => pl.Project is not null)
            .Select(async instance =>
            {
                var changeLogForPersonnel = await _orgClient.GetChangeLog(instance.Project.Id.ToString(),
                    instance.PositionId.ToString(), instance.InstanceId.ToString());

                var changeLogForPersonnelFilteredByLastSevenDays = changeLogForPersonnel.Events
                    .Where(e => e.TimeStamp > today.AddDays(-7).Date).ToList();

                var totalChanges = changeLogForPersonnelFilteredByLastSevenDays
                    .Where(ev => ev.Instance != null)
                    .Where(ev => ev.ChangeType == ChangeType.PositionInstancePercentChanged
                                 || ev.ChangeType == ChangeType.PositionInstanceLocationChanged
                                 || (ev.ChangeType == ChangeType.PositionInstanceAppliesFromChanged)
                                 || (ev.ChangeType == ChangeType.PositionInstanceAppliesToChanged))
                    .ToList()
                    .Count;

                Interlocked.Add(ref totalChangesForDepartment, totalChanges);
            });

        await Task.WhenAll(tasks);
        return totalChangesForDepartment;
    }

    private static int GetChangesAwaitingTaskOwnerAction(IEnumerable<ResourceAllocationRequest> listOfRequests)
    => listOfRequests
        .Where((req => req.Type is "ResourceOwnerChange"))
    .Where(req => req.State != null && req.State.Equals("created", StringComparison.OrdinalIgnoreCase)).ToList().Count();


    private static string CalculateAverageTimeToHandleRequests(IEnumerable<ResourceAllocationRequest> listOfRequests)
    {
        /*
         * How to calculate:
         * Find the workflow "created" and then find the date
         * This should mean that task owner have created and sent the request to resource owner
         * Find the workflow "proposal" and then find the date
         * This should mean that the resource owner have done their bit
         */

        var requestsHandledByResourceOwner = 0;
        var totalNumberOfDays = 0.0;
        var averageTimeUsedToHandleRequest = "0";

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
            if (dateForCreation == null || dateForApproval == null)
                continue;

            requestsHandledByResourceOwner++;
            var timespanDifference = dateForApproval - dateForCreation;
            var differenceInDays = timespanDifference.Value.TotalDays;
            totalNumberOfDays += differenceInDays;
        }

        if (!(totalNumberOfDays > 0))
            return averageTimeUsedToHandleRequest;

        var averageAmountOfTimeDouble = totalNumberOfDays / requestsHandledByResourceOwner;
        // To get whole number
        var averageAmountOfTimeInt = Convert.ToInt32(averageAmountOfTimeDouble);

        averageTimeUsedToHandleRequest = averageAmountOfTimeInt >= 1
            ? averageAmountOfTimeInt + " day(s)"
            : "Less than a day";

        return averageTimeUsedToHandleRequest;
    }

    private PersonnelContent CreatePersonnelContentWithPositionInformation(InternalPersonnelPerson person)
    {
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

    private static PersonnelContent CreatePersonnelContentWithTotalWorkload(InternalPersonnelPerson person)
    {
        var totalWorkLoad = person.ApiPersonAbsences?
            .Where(ab => ab.Type != ApiAbsenceType.Absence && ab.IsActive).Select(ab => ab.AbsencePercentage).Sum();
        totalWorkLoad += person.PositionInstances?.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum();

        var personnelContent = new PersonnelContent()
        {
            FullName = person.Name,
            TotalWorkload = totalWorkLoad,
        };

        return personnelContent;
    }

    private async Task<AdaptiveCard> ResourceOwnerAdaptiveCardBuilder(ResourceOwnerAdaptiveCardData cardData,
        string departmentIdentifier, string departmentSapId)
    {
        var personnelAllocationUri = $"{PortalUri()}apps/personnel-allocation/{departmentSapId}";
        var endingPositionsObjectList = cardData.PersonnelPositionsEndingWithNoFutureAllocation
            .Select(ep => new List<ListObject>
            {
                new()
                {
                    Value = ep.FullName,
                    Alignment = AdaptiveHorizontalAlignment.Left
                },
                new()
                {
                    Value = $"End date: {ep.EndingPosition?.AppliesTo?.ToString("dd/MM/yyyy")}",
                    Alignment = AdaptiveHorizontalAlignment.Right
                }
            })
            .ToList();
        var personnelMoreThan100PercentObjectList = cardData.PersonnelAllocatedMoreThan100Percent
            .Select(ep => new List<ListObject>
            {
                new()
                {
                    Value = ep.FullName,
                    Alignment = AdaptiveHorizontalAlignment.Left
                },
                new()
                {
                    Value = $"{ep.TotalWorkload} %",
                    Alignment = AdaptiveHorizontalAlignment.Right
                }
            })
            .ToList();

        var card = new AdaptiveCardBuilder()
            .AddHeading($"**Weekly summary - {departmentIdentifier}**")
            .AddColumnSet(new AdaptiveCardColumn(cardData.TotalNumberOfPersonnel.ToString(), "Number of personnel"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.CapacityInUse.ToString(), "Capacity in use",
                "%"))
            .AddColumnSet(
                new AdaptiveCardColumn(cardData.NumberOfRequestsLastWeek.ToString(), "New requests last week"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.NumberOfOpenRequests.ToString(), "Open requests"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.NumberOfRequestsStartingInLessThanThreeMonths.ToString(),
                "Requests with start date < 3 months"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.NumberOfRequestsStartingInMoreThanThreeMonths.ToString(),
                "Requests with start date > 3 months"))
            .AddColumnSet(new AdaptiveCardColumn(
                cardData.AverageTimeToHandleRequests,
                "Average time to handle request"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.AllocationChangesAwaitingTaskOwnerAction.ToString(),
                "Allocation changes awaiting task owner action"))
            .AddColumnSet(new AdaptiveCardColumn(cardData.ProjectChangesAffectingNextThreeMonths.ToString(),
                "Project changes last week affecting next 3 months"))
            .AddListContainer("Allocations ending soon with no future allocation:", endingPositionsObjectList)
            .AddListContainer("Personnel with more than 100% workload:", personnelMoreThan100PercentObjectList)
            .AddNewLine()
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