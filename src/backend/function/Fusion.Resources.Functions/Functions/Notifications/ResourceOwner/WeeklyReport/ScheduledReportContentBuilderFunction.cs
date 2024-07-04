using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.Functions.Notifications.ResourceOwner.WeeklyReport.DTOs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Fusion.Resources.Functions.Functions.Notifications.AdaptiveCardBuilder;
using Fusion.Integration.Profile;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using static Fusion.Resources.Functions.Common.ApiClients.IResourcesApiClient;

namespace Fusion.Resources.Functions.Functions.Notifications.ResourceOwner.WeeklyReport;

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

            if (!dto.AzureUniqueId.Any())
                throw new Exception("There are no recipients. This should have been filtered");
            
            if (string.IsNullOrEmpty(dto.FullDepartment))
                throw new Exception("FullDepartmentIdentifier not valid.");

            await BuildContentForResourceOwner(dto.AzureUniqueId, dto.FullDepartment, dto.DepartmentSapId);

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

    private async Task BuildContentForResourceOwner(IEnumerable<string> azureUniqueIds, string fullDepartment, string departmentSapId)
    {
        //  Requests for department
        var departmentRequests = (await _resourceClient.GetAllRequestsForDepartment(fullDepartment)).ToList();

        // Personnel for the department
        var departmentPersonnel =
            (await GetPersonnelForDepartmentExludingConsultantAndExternal(fullDepartment)).ToList();

        // Create notification card
        var card = CreateResourceOwnerAdaptiveCard(departmentPersonnel, departmentRequests, fullDepartment, departmentSapId);

        foreach (var azureUniqueIdStr in azureUniqueIds)
        {
            Guid azureUniqueId = Guid.Empty;

            if(!Guid.TryParse(azureUniqueIdStr, out azureUniqueId))
            {
                _logger.LogError($"Unable to parse notification recipient '{azureUniqueIdStr}'");

                continue;
            }

            if( azureUniqueId.Equals(Guid.Empty) )
            {
                _logger.LogError($"Empty notification recipient '{azureUniqueIdStr}'");

                continue;
            }

            await SendNotification(fullDepartment, card, azureUniqueId);
        }
    }


    private async Task<IEnumerable<InternalPersonnelPerson>> GetPersonnelForDepartmentExludingConsultantAndExternal(
        string fullDepartment)
    {
        var personnel = (await _resourceClient.GetAllPersonnelForDepartment(fullDepartment)).ToList();
        if (!personnel.Any())
            throw new Exception("No personnel found for department");

        var personnelWithoutConsultant = personnel.Where(per =>
            per.AccountType != FusionAccountType.Consultant.ToString() &&
            per.AccountType != FusionAccountType.External.ToString()).ToList();

        return personnelWithoutConsultant;
    }

    private async Task<List<ApiChangeLogEvent>> GetChangeLogEvents(
        IEnumerable<InternalPersonnelPerson> listOfInternalPersonnel)
    {
        var threeMonthsFromToday = DateTime.UtcNow.AddMonths(3);
        var today = DateTime.UtcNow;

        var listOfInternalPersonnelWithOnlyActiveProjects = listOfInternalPersonnel
            .SelectMany(per => per.PositionInstances.Where(pis =>
                (pis.AppliesFrom < threeMonthsFromToday && pis.AppliesFrom > today) || pis.AppliesTo > today))
            .ToList();

        var distinctProjectId = listOfInternalPersonnelWithOnlyActiveProjects.Select(p => p.Project?.Id).Distinct();
        var listAllRelevantInstanceIds = listOfInternalPersonnelWithOnlyActiveProjects.Select(x => x.InstanceId);

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = 3
        };

        var data = new ConcurrentDictionary<string, ApiChangeLog>();
        await Parallel.ForEachAsync(distinctProjectId, parallelOptions, async (project, _) =>
        {
            var changeLogForPersonnel = await _orgClient.GetChangeLog(project.ToString(), today.AddDays(-7));
            data.TryAdd(project.ToString(), changeLogForPersonnel);
        });

        return (from value in data.Values.ToList()
                from item in value.Events
                where listAllRelevantInstanceIds.Contains(item.InstanceId)
                select item).ToList();
    }

    private AdaptiveCard CreateResourceOwnerAdaptiveCard(
        List<InternalPersonnelPerson> personnel,
        List<ResourceAllocationRequest> requests,
        string departmentIdentifier, string departmentSapId)
    {
        var personnelAllocationUri = $"{PortalUri()}apps/personnel-allocation/{departmentSapId}";
        var endingPositionsObjectList = ResourceOwnerReportDataCreator
            .GetPersonnelPositionsEndingWithNoFutureAllocation(personnel)
            .Select(ep => new List<ListObject>
            {
                new()
                {
                    Value = ep.FullName,
                    Alignment = AdaptiveHorizontalAlignment.Left
                },
                new()
                {
                    Value = ep.EndDate is null ? "No end date" : $"End date: {ep.EndDate.Value:dd/MM/yyyy}",
                    Alignment = AdaptiveHorizontalAlignment.Right
                }
            })
            .ToList();
        var personnelMoreThan100PercentObjectList = ResourceOwnerReportDataCreator
            .GetPersonnelAllocatedMoreThan100Percent(personnel)
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
        var averageTimeToHandleRequests = ResourceOwnerReportDataCreator.GetAverageTimeToHandleRequests(requests);
        var card = new AdaptiveCardBuilder()
            .AddHeading($"**Weekly summary - {departmentIdentifier}**")
            .AddColumnSet(new AdaptiveCardColumn(
                ResourceOwnerReportDataCreator.GetTotalNumberOfPersonnel(personnel).ToString(),
                "Number of personnel (employees and external hire)"))
            .AddColumnSet(new AdaptiveCardColumn(
                ResourceOwnerReportDataCreator.GetCapacityInUse(personnel).ToString(),
                "Capacity in use",
                "%"))
            .AddColumnSet(
                new AdaptiveCardColumn(
                    ResourceOwnerReportDataCreator.GetNumberOfRequestsLastWeek(requests).ToString(),
                    "New requests last week"))
            .AddColumnSet(new AdaptiveCardColumn(
                ResourceOwnerReportDataCreator.GetNumberOfOpenRequests(requests).ToString(),
                "Open requests"))
            .AddColumnSet(new AdaptiveCardColumn(
                ResourceOwnerReportDataCreator.GetNumberOfRequestsStartingInLessThanThreeMonths(requests).ToString(),
                "Requests with start date < 3 months"))
            .AddColumnSet(new AdaptiveCardColumn(
                ResourceOwnerReportDataCreator.GetNumberOfRequestsStartingInMoreThanThreeMonths(requests).ToString(),
                "Requests with start date > 3 months"))
            .AddColumnSet(new AdaptiveCardColumn(
                averageTimeToHandleRequests > 0
                    ? averageTimeToHandleRequests + " day(s)"
                    : "Less than a day",
                "Average time to handle request (last 12 months)"))
            .AddColumnSet(new AdaptiveCardColumn(
                ResourceOwnerReportDataCreator.GetAllocationChangesAwaitingTaskOwnerAction(requests).ToString(),
                "Allocation changes awaiting task owner action"))
            .AddColumnSet(new AdaptiveCardColumn(
                ResourceOwnerReportDataCreator.CalculateDepartmentChangesLastWeek(personnel).ToString(),
                "Project changes last week affecting next 3 months"))
            .AddListContainer("Allocations ending soon with no future allocation:", endingPositionsObjectList)
            .AddListContainer("Personnel with more than 100% workload:", personnelMoreThan100PercentObjectList)
            .AddNewLine()
            .AddActionButton("Go to Personnel allocation app", personnelAllocationUri)
            .Build();

        return card;
    }

    private async Task SendNotification(string fullDepartment, AdaptiveCard card, Guid azureUniqueId)
    {
        var sendNotification = await _notificationsClient.SendNotification(
                new SendNotificationsRequest
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

    private string PortalUri()
    {
        var portalUri = _configuration["Endpoints_portal"] ?? "https://fusion.equinor.com/";
        if (!portalUri.EndsWith("/"))
            portalUri += "/";
        return portalUri;
    }
}