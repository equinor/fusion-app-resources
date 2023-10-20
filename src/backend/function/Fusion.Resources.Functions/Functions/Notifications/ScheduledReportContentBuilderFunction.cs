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
            if (dto.FullDepartment.IsNullOrEmpty())
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
        var departmentRequestWithMoreThanThreeMonthsBeforeStart = departmentRequests.Where(x => x.State != "completed" && x.OrgPositionInstance.AppliesFrom > threeMonthsFuture).Count();

        // We use the full list of requets and filter to only inlclude the ones which have start-date in less than 3 months and start-date after today and is not complete and has no proposedPerson assigned to them
        // 3. Number of requests that are less than 3 month to start data with no nomination.
        // FIXME: M� justeres p�.
        var departmentRequestsWithLessThanThreeMonthsBeforeStartAndNoNomination = departmentRequests.Where(x => x.State != "completed" && (x.OrgPositionInstance.AppliesFrom < threeMonthsFuture && x.OrgPositionInstance.AppliesFrom > today) && !x.HasProposedPerson).Count();

        // Only to include those requests which have state approval (this means that the resource owner needs to process the requests in some way)
        // 4. Number of open requests.  
        // FIXME: Needs to be checked
        var totalNumberOfOpenRequests = departmentRequests.Where(x => x.State != "completed").Count();


        // ##Get all the personnel for the specific department
        var listOfPersonnelForDepartment = await _resourceClient.GetAllPersonnelForDepartment(departmentIdentifier);
        var personnelForDepartment = listOfPersonnelForDepartment.Value.ToList();


        //5. List with personnel positions ending within 3 months and with no future allocation (link to personnel allocation)
        var listOfPersonnelPositionsEndingInThreeMonths = personnelForDepartment.Where(x => x.PositionInstances.Where(pos => pos.IsActive && pos.AppliesTo <= threeMonthsFuture).Any()); // Her tar man utgangspunkt i at den n�v�rende splitten ogs� er satt til IsActive
        var newListWithPersonnelWithoutFutureAllocations = listOfPersonnelPositionsEndingInThreeMonths.Where(person => person.PositionInstances.All(pos => pos.AppliesTo < threeMonthsFuture));
        IEnumerable<PersonnelContent> listOfPersonnelWithoutFutureAllocations = newListWithPersonnelWithoutFutureAllocations.Select(p => CreatePersonnelContent(p));


        // 6. Number of personnel allocated more than 100 %
        var listOfPersonnelsWithMoreThan100Percent = personnelForDepartment.Where(p => p.PositionInstances.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum() > 100);
        IEnumerable<PersonnelContent> listOfPersonnelForDepartmentWithMoreThan100Percent = listOfPersonnelsWithMoreThan100Percent.Select(p => CreatePersonnelWithTBEContent(p)); //


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
            PersonnelAllocatedMoreThan100Percent = listOfPersonnelForDepartmentWithMoreThan100Percent,
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


    // Forel�pig uten � ta med LEAVE i beregningen
    public int FindTotalPercentagesAllocatedOfTotal(List<ApiInternalPersonnelPerson> listOfInternalPersonnel)
    {
        double totalWorkLoad = 0.0;
        foreach (var personnel in listOfInternalPersonnel)
        {
            totalWorkLoad += personnel.PositionInstances.Where(pos => pos.IsActive).Select(pos => pos.Workload).Sum();
        }

        double totalPercentage = totalWorkLoad / (listOfInternalPersonnel.Count * 100) * 100;


        return Convert.ToInt32(totalPercentage);
    }

    public PersonnelContent CreatePersonnelContent(ApiInternalPersonnelPerson person)
    {
        if (person == null)
            throw new ArgumentNullException();

        var position = person.PositionInstances.Find(instance => instance.IsActive);
        var positionName = position.Name; // Tar utgangspunkt i at n�rv�rende splitt g�r ut innen 3 m�neder og det ikke er noe flere pga filtreringen som gj�res i forkant
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

    public PersonnelContent CreatePersonnelWithTBEContent(ApiInternalPersonnelPerson person)
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
        public string? PositionName { get; set; }
        public double? TotalWorkload { get; set; }
        public int? NumberOfPositionInstances { get; set; }
        public PersonnelPosition? EndingPosition { get; set; }

        public PersonnelContent() { }
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
        adaptiveCard = AddTextToAdaptiveCardWithColumns1(adaptiveCard, "Capacity in use", "%", cardData.PercentAllocationOfTotalCapacity.ToString());
        adaptiveCard = AddTextToAdaptiveCardWithColumns1(adaptiveCard, "Total requests", "", cardData.TotalNumberOfRequests.ToString());
        adaptiveCard = AddTextToAdaptiveCardWithColumns1(adaptiveCard, "Open requests", "", cardData.NumberOfOpenRequests.ToString());
        adaptiveCard = AddTextToAdaptiveCardWithColumns1(adaptiveCard, "Requests with start date less than 3 months", "", cardData.NumberOfNewRequestsWithNoNomination.ToString());
        adaptiveCard = AddTextToAdaptiveCardWithColumns1(adaptiveCard, "Requests with start date more than 3 months", "", cardData.NumberOfOlderRequests.ToString());
        adaptiveCard = AddTextToAdaptiveCardWithColumns2(adaptiveCard, "Positions ending soon with no future allocation", "End date: ", cardData);
        adaptiveCard = AddTextToAdaptiveCardWithColumns3(adaptiveCard, "Personnel with more than 100% FTE:", "% FTE", cardData);

        return adaptiveCard;
    }

    public static AdaptiveCard AddTextToAdaptiveCardWithColumns1(AdaptiveCard adaptiveCard, string customtext1, string? customtext2, string value)
    {

        // F�rste container med 2 kolonner
        var container = new AdaptiveContainer();
        container.Separator = true;
        // KolonneSett 1 med 2 kolonner
        var columnSet1 = new AdaptiveColumnSet();
        // Kolonne 1
        var column1 = new AdaptiveColumn();
        column1.Width = AdaptiveColumnWidth.Stretch;
        column1.Separator = true;
        column1.Spacing = AdaptiveSpacing.Medium;

        var textBlock1 = new AdaptiveTextBlock();
        textBlock1.Text = customtext1;
        textBlock1.Wrap = true;
        textBlock1.HorizontalAlignment = AdaptiveHorizontalAlignment.Center;

        var textBlock2 = new AdaptiveTextBlock()
        {
            Wrap = true,
            Text = value + customtext2,
            Size = AdaptiveTextSize.ExtraLarge,
            HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
        };

        column1.Add(textBlock2);
        column1.Add(textBlock1);
        columnSet1.Add(column1);
        container.Add(columnSet1);
        adaptiveCard.Body.Add(container);


        return adaptiveCard;
    }

    public static AdaptiveCard AddTextToAdaptiveCardWithColumns2(AdaptiveCard adaptiveCard, string customtext1, string customtext2, ResourceOwnerAdaptiveCardData carddata)
    {
        var container = new AdaptiveContainer()
        { Separator = true };

        var columnSet1 = new AdaptiveColumnSet();

        var column1 = new AdaptiveColumn();
        var textBlock1 = new AdaptiveTextBlock()
        {
            Text = customtext1,
            Wrap = true,
            Size = AdaptiveTextSize.Default,
            Weight = AdaptiveTextWeight.Bolder
        };
        column1.Add(textBlock1);

        foreach(var p in carddata.PersonnelPositionsEndingWithNoFutureAllocation)
        {
            var columnset2 = new AdaptiveColumnSet();

            var column2 = new AdaptiveColumn();
            var textBlock2 = new AdaptiveTextBlock()
            {
                Text = p.FullName,
                Wrap = true,
                Size = AdaptiveTextSize.Default,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Left
            };


            column2.Add(textBlock2);
            columnset2.Add(column2);

            var column3 = new AdaptiveColumn();
            var textBlock3 = new AdaptiveTextBlock()
            {
                Text = "End date: " + p.EndingPosition.AppliesTo.Value.Date.ToShortDateString(),
                Wrap = true,
                Size = AdaptiveTextSize.Default,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Right
            };

            column3.Add(textBlock3);
            columnset2.Add(column3);
            column1.Add(columnset2);
        }


        columnSet1.Add(column1);
        container.Add(columnSet1);

        adaptiveCard.Body.Add(container);

        return adaptiveCard;
    }

    public static AdaptiveCard AddTextToAdaptiveCardWithColumns3(AdaptiveCard adaptiveCard, string customtext1, string customtext2, ResourceOwnerAdaptiveCardData carddata)
    {
        var container = new AdaptiveContainer()
        { Separator = true };

        var columnSet1 = new AdaptiveColumnSet();

        var column1 = new AdaptiveColumn();
        var textBlock1 = new AdaptiveTextBlock()
        {
            Text = customtext1,
            Wrap = true,
            Size = AdaptiveTextSize.Default,
            Weight = AdaptiveTextWeight.Bolder
        };
        column1.Add(textBlock1);

        foreach (var p in carddata.PersonnelAllocatedMoreThan100Percent)
        {
            var columnset2 = new AdaptiveColumnSet();

            var column2 = new AdaptiveColumn();
            var textBlock2 = new AdaptiveTextBlock()
            {
                Text = p.FullName,
                Wrap = true,
                Size = AdaptiveTextSize.Default,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Left
            };


            column2.Add(textBlock2);
            columnset2.Add(column2);

            var column3 = new AdaptiveColumn();
            var textBlock3 = new AdaptiveTextBlock()
            {
                Text = p.TotalWorkload + customtext2,
                Wrap = true,
                Size = AdaptiveTextSize.Default,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Right
            };

            column3.Add(textBlock3);
            columnset2.Add(column3);
            column1.Add(columnset2);
        }


        columnSet1.Add(column1);
        container.Add(columnSet1);

        adaptiveCard.Body.Add(container);

        return adaptiveCard;
    }
}