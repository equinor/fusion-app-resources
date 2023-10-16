using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.ApiClients.ApiModels;
using Fusion.Resources.Functions.Functions.Notifications.Models.AdaptiveCard_Models;
using Fusion.Resources.Functions.Functions.Notifications.Models.DTOs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using static Fusion.Resources.Functions.ApiClients.IResourcesApiClient;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportContentBuilderFunction
{
    private readonly ILogger<ScheduledReportContentBuilderFunction> _logger;
    private readonly string _queueName;
    private readonly INotificationApiClient _notificationsClient;
    private readonly IResourcesApiClient _resourceClient;

        public ScheduledReportContentBuilderFunction(ILogger<ScheduledReportContentBuilderFunction> logger,
        IConfiguration configuration, IResourcesApiClient resourcesApiClient,
         INotificationApiClient notificationsClient)
    {
        _logger = logger;
        _queueName = configuration["scheduled_notification_report_queue"];
        _resourceClient = resourcesApiClient;
        _notificationsClient = notificationsClient;
    }

    [FunctionName(ScheduledReportFunctionSettings.ContentBuilderFunctionName)]
    public async Task RunAsync(
        [ServiceBusTrigger("%scheduled_notification_report_queue%", Connection = "AzureWebJobsServiceBus")]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageReceiver)
    {
        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' " +
            $"started with message: {message.Body}");
        try
        {
            var body = Encoding.UTF8.GetString(message.Body);
            var dto = JsonConvert.DeserializeObject<ScheduledNotificationQueueDto>(body);
            if (!Guid.TryParse(dto.AzureUniqueId, out var azureUniqueId))
                throw new Exception("AzureUniqueId not valid.");
            if(dto.FullDepartment.IsNullOrEmpty())
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

            // TODO: The message should be completed after the email has been sent.
            await messageReceiver.CompleteMessageAsync(message);

            _logger.LogInformation(
                $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' " +
                $"finished with message: {message.Body}");
        }
        catch (Exception e)
        {
            _logger.LogError(
                $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' " +
                $"failed with exception: {e.Message}");
        }
    }

    private async Task BuildContentForTaskOwner(Guid azureUniqueId)
    {
        throw new NotImplementedException();
    }

    private async Task BuildContentForResourceOwner(Guid azureUniqueId, string departmentIdentifier)
    {
        // TODO: HardCoded for testing purposes.
        //const string davidAzureUniqueId = "945f666e-fd8a-444c-b7e3-9da61b21e4b5";
        //azureUniqueId = Guid.Parse(davidAzureUniqueId);

        const string aleksanderAzureUniqueId = "f9158061-e8e3-494a-acbe-afcb6bc9f7ab";
        azureUniqueId = Guid.Parse(aleksanderAzureUniqueId);

        var threeMonthsFuture = DateTime.UtcNow.AddMonths(3);
        var today = DateTime.UtcNow;




        // DepartmentIdentifier = PDP PRD PMC PCA;
        // Get all requests for specific Department regardsless of state
        var departmentRequestsResponse = await _resourceClient.GetAllRequestsForDepartment(departmentIdentifier);
        var departmentRequests = departmentRequestsResponse.Value.ToList();


        // Count all of the number of requests sent to the department. We may change this to only include a specific timeframe in the future (last 12 months)
        // 1. Total number of request sent to department -  OK!
        var totalNumberOfRequests = departmentRequests.Count();

        // We use the full list of requests and filter to only include the ones which are to have start-date in more than 3 months AND state not completed
        // TODO: Also get a way to show link for this content...
        // 2. Number of request that have more than 3 months to start data(link to system with filtered view)
        var departmentRequestWithMoreThanThreeMonthsBeforeStart = departmentRequests.Where(x => x.State != "complete" && x.OrgPositionInstance.AppliesFrom > threeMonthsFuture).Count();

        // We use the full list of requets and filter to only inlclude the ones which have start-date in less than 3 months and start-date after today and is not complete and has no proposedPerson assigned to them
        // 3. Number of requests that are less than 3 month to start data with no nomination.
        // FIXME: Må justeres på.
        var departmentRequestsWithLessThanThreeMonthsBeforeStartAndNoNomination = departmentRequests.Where(x => x.State != "complete" && (x.OrgPositionInstance.AppliesFrom < threeMonthsFuture && x.OrgPositionInstance.AppliesFrom > today) && !x.HasProposedPerson).Count();

        // Only to include those requests which have state approval (this means that the resource owner needs to process the requests in some way)
        // 4. Number of open requests.  
        // FIXME: Needs to be checked
        var totalNumberOfOpenRequests = departmentRequests.Where(x => x.State == "approval").Count();


        // ##Get all the personnel for the specific department
        var listOfPersonnelForDepartment = await _resourceClient.GetAllPersonnelForDepartment(departmentIdentifier);
        var personnelForDepartment = listOfPersonnelForDepartment.Value.ToList();




        //5. List with personnel positions ending within 3 months and with no future allocation (link to personnel allocation)
        // WORK IN PROGRESS
        // FIXME: Needs to get the following:
        // FullName of user + PositionName + ProjectName + The time the position is ending
        var listOfPersonnelPositionsEndingInThreeMonths = personnelForDepartment.Where(x => x.PositionInstances.Where(pos => pos.IsActive && pos.AppliesTo <= threeMonthsFuture).Any()); // Her tar man utgangspunkt i at den nåværende splitten også er satt til IsActive
        var newListWithPersonnelWithoutFutureAllocations = listOfPersonnelPositionsEndingInThreeMonths.Where(person => person.PositionInstances.All(pos => pos.AppliesTo < threeMonthsFuture));
        //List<string> listOfPersonnelWithoutFutureAllocations = newListWithPersonnelWithoutFutureAllocations.Select(p => p.Name.ToString()).ToList();
        IEnumerable<PersonnelContent> listOfPersonnelWithoutFutureAllocations = newListWithPersonnelWithoutFutureAllocations.Select(p => CreatePersonnelContent(p));


        // 6. Number of personnel allocated more than 100 %
        // WORK IN PROGRESS
        // FIXME: Needs to get the following:
        // Fullname of user + Total number of FTE + All the different allocations with the percentage allocated
        var listOfPersonnelsWithMoreThan100Percent = personnelForDepartment.Where(p => p.PositionInstances.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum() > 100);

        IEnumerable<PersonnelContent> listOfPersonnelForDepartmentWithMoreThan100Percent = listOfPersonnelsWithMoreThan100Percent.Select(p =>  CreatePersonnelWithTBEContent(p)); //


        //7. % of total allocation vs.capacity
        // WORK IN PROGRESS
        // FIXME: Needs the following:
        // The total number of allocations (number of personnel allocated (percentage) + the total possible number of personnel and their maximum possible percentage of allocation
        // Show this as a percentagenumber (in the first draft)
        int percentageOfTotalCapacity = FindTotalPercentagesAllocatedOfTotal(personnelForDepartment);



        //8.EXT Contracts ending within 3 months ? (data to be imported from SAP or AD) 
        // ContractPersonnel'et? - Knyttet til projectmaster -> Knyttet til orgkart
        // Skip this for now...


        var card = ResourceOwnerAdaptiveCardBuilder(new ResourceOwnerAdaptiveCardData
        {
            NumberOfOlderRequests = departmentRequestWithMoreThanThreeMonthsBeforeStart,
            NumberOfOpenRequests = totalNumberOfOpenRequests,
            NumberOfNewRequestsWithNoNomination = departmentRequestsWithLessThanThreeMonthsBeforeStartAndNoNomination,
            NumberOfExtContractsEnding = 4,
            ListOfPersonnelAllocatedMoreThan100Percent = listOfPersonnelForDepartmentWithMoreThan100Percent,
            PercentAllocationOfTotalCapacity = percentageOfTotalCapacity,
            TotalNumberOfRequests = totalNumberOfRequests,
            PersonnelPositionsEndingWithNoFutureAllocation = listOfPersonnelWithoutFutureAllocations,
        },
        departmentIdentifier);

        var sendNotification = await _notificationsClient.SendNotification(
            new SendNotificationsRequest()
            {
                Title = $"Weekly summary - {departmentIdentifier}",
                EmailPriority = 1,
                Card = card,
                Description = $"Weekly report for departmenty - {departmentIdentifier}"
            },
            azureUniqueId);

        // Throwing exception if the response is not successful.
        if (!sendNotification)
        {
            throw new Exception(
                $"Failed to send notification to resource-owner with AzureUniqueId: '{azureUniqueId}'.");
        }
    }


    // Foreløpig uten å ta med LEAVE i beregningen
    public int FindTotalPercentagesAllocatedOfTotal(List<ApiInternalPersonnelPerson> listOfInternalPersonnel)
    {
        double totalWorkLoad = 0.0;
        foreach (var personnel in listOfInternalPersonnel)
        {
            totalWorkLoad += personnel.PositionInstances.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum();
        }

        double totalPercentage = totalWorkLoad / (listOfInternalPersonnel.Count * 100) *  100;


        return Convert.ToInt32(totalPercentage); 
    }

    public PersonnelContent CreatePersonnelContent (ApiInternalPersonnelPerson person)
    {
        if (person == null)
            throw new ArgumentNullException();

        var position = person.PositionInstances.Find(instance => instance.IsActive);
        var positionName = position.Name; // Tar utgangspunkt i at nårværende splitt går ut innen 3 måneder og det ikke er noe flere pga filtreringen som gjøres i forkant
        var projectName = position.Project.Name;
        var personnelContent = new PersonnelContent()
        {
            FullName = person.Name,
            PositionName = positionName,
            ProjectName = projectName
        };
        return personnelContent;
    }

    public PersonnelContent CreatePersonnelWithTBEContent (ApiInternalPersonnelPerson person)
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

    

    public class PersonnelContent
    {
        public string FullName { get; set; }
        public string? ProjectName { get; set; }
        public string? PositionName{ get; set; }
        public double? TotalWorkload {  get; set; }
        public int? NumberOfPositionInstances { get; set; }

        public PersonnelContent () { }
    }    

    private static AdaptiveCard ResourceOwnerAdaptiveCardBuilder(ResourceOwnerAdaptiveCardData cardData, string departmentIdentifier)
    {
       var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));

        card.Body.Add(new AdaptiveTextBlock
        {
            Text = $"**Weekly summary - {departmentIdentifier}**",
            Size = AdaptiveTextSize.Large,
            Weight = AdaptiveTextWeight.Bolder,
            Wrap = true // Allow text to wrap
        });

        var facts = new List<string>
        {
        "**Total requests**: " + cardData.TotalNumberOfRequests,
        "**Requests starting after 3 months**: " + cardData.NumberOfOlderRequests,
        "**Requests starting within 3 months (no nomination)**: " + cardData.NumberOfNewRequestsWithNoNomination,
        "**Open requests**: " + cardData.NumberOfOpenRequests,
        "**Personnel positions ending within 3 months (No Future Allocation)**: " + string.Join(", ", cardData.PersonnelPositionsEndingWithNoFutureAllocation),
        "**Percent allocation of total capacity**: " + cardData.PercentAllocationOfTotalCapacity + "%",
        "**Personnel allocated more than 100%**: " + string.Join(", ",  cardData.ListOfPersonnelAllocatedMoreThan100Percent),
        "**EXT contracts ending in 3 months**: " + cardData.NumberOfExtContractsEnding
        };

        foreach (var fact in facts)
        {
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = fact,
                Wrap = true // Allow text to wrap
            });
        }
        return card;
    }
}