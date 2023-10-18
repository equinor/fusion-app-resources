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
        var listOfPersonnelPositionsEndingInThreeMonths = personnelForDepartment.Where(x => x.PositionInstances.Where(pos => pos.IsActive && pos.AppliesTo <= threeMonthsFuture).Any()); // Her tar man utgangspunkt i at den nåværende splitten også er satt til IsActive
        var newListWithPersonnelWithoutFutureAllocations = listOfPersonnelPositionsEndingInThreeMonths.Where(person => person.PositionInstances.All(pos => pos.AppliesTo < threeMonthsFuture));
        IEnumerable<PersonnelContent> listOfPersonnelWithoutFutureAllocations = newListWithPersonnelWithoutFutureAllocations.Select(p => CreatePersonnelContent(p));


        // 6. Number of personnel allocated more than 100 %
        var listOfPersonnelsWithMoreThan100Percent = personnelForDepartment.Where(p => p.PositionInstances.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum() > 100);
        IEnumerable<PersonnelContent> listOfPersonnelForDepartmentWithMoreThan100Percent = listOfPersonnelsWithMoreThan100Percent.Select(p =>  CreatePersonnelWithTBEContent(p)); //


        //7. % of total allocation vs.capacity
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
            ProjectName = projectName,
            EndingPosition = position
            
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
        public PersonnelPosition? EndingPosition { get; set; }

        public PersonnelContent () { }
    }

        private static AdaptiveCard ResourceOwnerAdaptiveCardBuilder(ResourceOwnerAdaptiveCardData cardData, string departmentIdentifier)
    {
       var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2));

        card.Body.Add(new AdaptiveTextBlock
        {
            Text = $"**Weekly summary - {departmentIdentifier}**",
            Size = AdaptiveTextSize.Large,
            Weight = AdaptiveTextWeight.Bolder,
            Wrap = true // Allow text to wrap
        });

        card = CreateAdaptiveCardTemp(card, cardData);

        return card;
    }


    // FIXME: Temporary way to compose a adaptive card. Needs refactoring
    public static AdaptiveCard CreateAdaptiveCardTemp(AdaptiveCard adaptiveCard, ResourceOwnerAdaptiveCardData cardData)
    {

        // Første container med 2 kolonner
        var container1 = new AdaptiveContainer();
        container1.Separator = true;
        // KolonneSett 1 med 2 kolonner
        var columnSet11 = new AdaptiveColumnSet();
        // Kolonne 1
        var column111 = new AdaptiveColumn();
        column111.Width = AdaptiveColumnWidth.Stretch;
        column111.Separator = true;
        column111.Spacing = AdaptiveSpacing.Medium;

        var textBlock1111 = new AdaptiveTextBlock();
        textBlock1111.Text = "Capacity in use";
        textBlock1111.Wrap = true;
        textBlock1111.HorizontalAlignment = AdaptiveHorizontalAlignment.Center;

        var textBlock1112 = new AdaptiveTextBlock();
        textBlock1112.Text = cardData.PercentAllocationOfTotalCapacity.ToString() + "%";
        textBlock1112.Wrap = true;
        textBlock1112.Size = AdaptiveTextSize.ExtraLarge;
        textBlock1112.HorizontalAlignment = AdaptiveHorizontalAlignment.Center;

        column111.Add(textBlock1112);
        column111.Add(textBlock1111);
        columnSet11.Add(column111);
        container1.Add(columnSet11);
        adaptiveCard.Body.Add(container1);



        // Første container med 2 kolonner
        var container2 = new AdaptiveContainer();
        container2.Separator = true;
        // KolonneSett 1 med 2 kolonner
        var columnSet21 = new AdaptiveColumnSet();
        // Kolonne 1
        var column211 = new AdaptiveColumn();
        column211.Width = AdaptiveColumnWidth.Stretch;
        column211.Separator = true;
        column211.Spacing = AdaptiveSpacing.Medium;

        var textBlock2111 = new AdaptiveTextBlock();
        textBlock2111.Text = "Total requests";
        textBlock2111.Wrap = true;
        textBlock2111.HorizontalAlignment = AdaptiveHorizontalAlignment.Center;

        var textBlock2112 = new AdaptiveTextBlock();
        textBlock2112.Text = cardData.TotalNumberOfRequests.ToString();
        textBlock2112.Wrap = true;
        textBlock2112.Size = AdaptiveTextSize.ExtraLarge;
        textBlock2112.HorizontalAlignment = AdaptiveHorizontalAlignment.Center;

        column211.Add(textBlock2112);
        column211.Add(textBlock2111);
        columnSet21.Add(column211);
        container2.Add(columnSet21);
        adaptiveCard.Body.Add(container2);



        // Første container med 2 kolonner
        var container3 = new AdaptiveContainer();
        container3.Separator = true;
        // KolonneSett 1 med 2 kolonner
        var columnSet31 = new AdaptiveColumnSet();
        // Kolonne 1
        var column311 = new AdaptiveColumn();
        column311.Width = AdaptiveColumnWidth.Stretch;
        column311.Separator = true;
        column311.Spacing = AdaptiveSpacing.Medium;

        var textBlock3111 = new AdaptiveTextBlock();
        textBlock3111.Text = "Open requests";
        textBlock3111.Wrap = true;
        textBlock3111.HorizontalAlignment = AdaptiveHorizontalAlignment.Center;

        var textBlock3112 = new AdaptiveTextBlock();
        textBlock3112.Text = cardData.NumberOfOpenRequests.ToString();
        textBlock3112.Wrap = true;
        textBlock3112.Size = AdaptiveTextSize.ExtraLarge;
        textBlock3112.HorizontalAlignment = AdaptiveHorizontalAlignment.Center;

        column311.Add(textBlock3112);
        column311.Add(textBlock3111);
        columnSet31.Add(column311);
        container3.Add(columnSet31);
        adaptiveCard.Body.Add(container3);


        // Første container med 2 kolonner
        var container4 = new AdaptiveContainer();
        container4.Separator = true;
        // KolonneSett 1 med 2 kolonner
        var columnSet41 = new AdaptiveColumnSet();
        // Kolonne 1
        var column411 = new AdaptiveColumn();
        column411.Width = AdaptiveColumnWidth.Stretch;
        column411.Separator = true;
        column411.Spacing = AdaptiveSpacing.Medium;

        var textBlock4111 = new AdaptiveTextBlock();
        textBlock4111.Text = "Requests with start date less than 3 months";
        textBlock4111.Wrap = true;
        textBlock4111.HorizontalAlignment = AdaptiveHorizontalAlignment.Center;

        var textBlock4112 = new AdaptiveTextBlock();
        textBlock4112.Text = cardData.NumberOfNewRequestsWithNoNomination.ToString();
        textBlock4112.Wrap = true;
        textBlock4112.Size = AdaptiveTextSize.ExtraLarge;
        textBlock4112.HorizontalAlignment = AdaptiveHorizontalAlignment.Center;

        column411.Add(textBlock4112);
        column411.Add(textBlock4111);
        columnSet41.Add(column411);
        container4.Add(columnSet41);
        adaptiveCard.Body.Add(container4);


        //#######################



        // Allocations ending soon with no future allocation
        //Foreach personnel with position
        var container5 = new AdaptiveContainer();
        container5.Separator = true;

        var columnSet3 = new AdaptiveColumnSet();
        columnSet3.Separator = true;

        var column31 = new AdaptiveColumn();
        column31.Separator = true;
        var textBlock311 = new AdaptiveTextBlock();
        textBlock311.Text = "Positions ending soon with no future allocation";
        textBlock311.Wrap = true;
        textBlock311.Size = AdaptiveTextSize.Default;
        textBlock311.Weight = AdaptiveTextWeight.Bolder;
        column31.Add(textBlock311);
        var columnset31 = new AdaptiveColumnSet();

        var column32 = new AdaptiveColumn();
        var textBlock321 = new AdaptiveTextBlock();
        textBlock321.Wrap = true;
        textBlock321.Size = AdaptiveTextSize.Default;
        textBlock321.HorizontalAlignment = AdaptiveHorizontalAlignment.Left;
        textBlock321.Text = cardData.PersonnelPositionsEndingWithNoFutureAllocation.FirstOrDefault().FullName;
        column32.Add(textBlock321);
        columnset31.Add(column32);

        var column33 = new AdaptiveColumn();
        var textBlock331 = new AdaptiveTextBlock();
        textBlock331.Wrap = true;
        textBlock331.Size = AdaptiveTextSize.Default;
        textBlock331.HorizontalAlignment = AdaptiveHorizontalAlignment.Right;
        textBlock331.Text = "End date: " + cardData.PersonnelPositionsEndingWithNoFutureAllocation.FirstOrDefault().EndingPosition.AppliesTo.Value.Date.ToShortDateString();
        column33.Add(textBlock331);
        columnset31.Add(column33);
        column31.Add(columnset31);

        columnSet3.Add(column31);
        container5.Add(columnSet3);

        adaptiveCard.Body.Add(container5);

        //#######################


        // Allocations ending soon with no future allocation
        //Foreach personnel with position
        var container6 = new AdaptiveContainer();
        container6.Separator = true;

        var columnSet4 = new AdaptiveColumnSet();

        var column41 = new AdaptiveColumn();
        var textBlock411 = new AdaptiveTextBlock();
        textBlock411.Text = "Personnel with more than 100% FTE:";
        textBlock411.Wrap = true;
        textBlock411.Size = AdaptiveTextSize.Default;
        textBlock411.Weight = AdaptiveTextWeight.Bolder;
        column41.Add(textBlock411);
        var columnset41 = new AdaptiveColumnSet();

        var column42 = new AdaptiveColumn();
        var textBlock421 = new AdaptiveTextBlock();
        textBlock421.Wrap = true;
        textBlock421.Size = AdaptiveTextSize.Default;
        textBlock421.HorizontalAlignment = AdaptiveHorizontalAlignment.Left;
        textBlock421.Text = cardData.ListOfPersonnelAllocatedMoreThan100Percent.FirstOrDefault().FullName;
        column42.Add(textBlock421);
        columnset41.Add(column42);

        var column43 = new AdaptiveColumn();
        var textBlock431 = new AdaptiveTextBlock();
        textBlock431.Wrap = true;
        textBlock431.Size = AdaptiveTextSize.Default;
        textBlock431.HorizontalAlignment = AdaptiveHorizontalAlignment.Right;
        textBlock431.Text = cardData.ListOfPersonnelAllocatedMoreThan100Percent.FirstOrDefault().TotalWorkload + "% FTE";
        column43.Add(textBlock431);
        columnset41.Add(column43);
        column41.Add(columnset41);

        columnSet4.Add(column41);
        container6.Add(columnSet4);

        adaptiveCard.Body.Add(container6);

        return adaptiveCard;
    }
}