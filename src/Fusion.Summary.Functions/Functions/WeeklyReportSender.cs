using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static Fusion.Summary.Functions.Functions.WeeklyReportSender.AdaptiveCardBuilder;

namespace Fusion.Summary.Functions.Functions;

public class WeeklyReportSender
{
    private readonly ISummaryApiClient summaryApiClient;
    private readonly INotificationApiClient notificationApiClient;
    private readonly ILogger<WeeklyReportSender> logger;
    private readonly IConfiguration configuration;


    public WeeklyReportSender(ISummaryApiClient summaryApiClient, INotificationApiClient notificationApiClient,
        ILogger<WeeklyReportSender> logger, IConfiguration configuration)
    {
        this.summaryApiClient = summaryApiClient;
        this.notificationApiClient = notificationApiClient;
        this.logger = logger;
        this.configuration = configuration;
    }

    [FunctionName("weekly-report-sender")]
    public async Task RunAsync([TimerTrigger("0 0 8 * * 1", RunOnStartup = false)] TimerInfo timerInfo)
    {
        var departments = await summaryApiClient.GetDepartmentsAsync();


        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 10
        };

        await Parallel.ForEachAsync(departments, options, async (department, ct) =>
        {
            var summaryReport = await summaryApiClient.GetLatestWeeklyReportAsync(department.DepartmentSapId, ct);

            if (summaryReport is null)
            {
                logger.LogCritical(
                    "No summary report found for departmentSapId {@Department}. Unable to send report notification",
                    department);
                return;
            }

            var notification = CreateNotification(summaryReport, department);

            await notificationApiClient.SendNotification(notification, department.ResourceOwnerAzureUniqueId);
        });
    }


    private SendNotificationsRequest CreateNotification(ApiSummaryReport report, ApiResourceOwnerDepartment department)
    {
        var personnelAllocationUri = $"{PortalUri()}apps/personnel-allocation/{department.DepartmentSapId}";
        var endingPositionsObjectList = report.PositionsEnding
            .Select(ep => new List<ListObject>
            {
                new()
                {
                    Value = ep.FullName,
                    Alignment = AdaptiveHorizontalAlignment.Left
                },
                new()
                {
                    Value = ep.EndDate == DateTime.MinValue
                        ? "No end date"
                        : $"End date: {ep.EndDate:dd/MM/yyyy}",
                    Alignment = AdaptiveHorizontalAlignment.Right
                }
            })
            .ToList();
        var personnelMoreThan100PercentObjectList = report.PersonnelMoreThan100PercentFTE
            .Select(ep => new List<ListObject>
            {
                new()
                {
                    Value = ep.FullName,
                    Alignment = AdaptiveHorizontalAlignment.Left
                },
                new()
                {
                    Value = $"{ep.FTE} %",
                    Alignment = AdaptiveHorizontalAlignment.Right
                }
            })
            .ToList();

        // TODO: Is this in days?
        var averageTimeToHandleRequests = TimeSpan.TryParse(report.AverageTimeToHandleRequests, out var timeSpan)
            ? timeSpan.Days
            : int.Parse(report.AverageTimeToHandleRequests);

        var card = new AdaptiveCardBuilder()
            .AddHeading($"**Weekly summary - {department.FullDepartmentName}**")
            .AddColumnSet(new AdaptiveCardColumn(
                report.NumberOfPersonnel,
                "Number of personnel (employees and external hire)"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.CapacityInUse,
                "Capacity in use",
                "%"))
            .AddColumnSet(
                new AdaptiveCardColumn(
                    report.NumberOfRequestsLastPeriod,
                    "New requests last week"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.NumberOfOpenRequests,
                "Open requests"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.NumberOfRequestsStartingInLessThanThreeMonths,
                "Requests with start date < 3 months"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.NumberOfRequestsStartingInMoreThanThreeMonths,
                "Requests with start date > 3 months"))
            .AddColumnSet(new AdaptiveCardColumn(
                averageTimeToHandleRequests > 0
                    ? averageTimeToHandleRequests + " day(s)"
                    : "Less than a day",
                "Average time to handle request (last 12 months)"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.AllocationChangesAwaitingTaskOwnerAction,
                "Allocation changes awaiting task owner action"))
            .AddColumnSet(new AdaptiveCardColumn(
                report.ProjectChangesAffectingNextThreeMonths,
                "Project changes last week affecting next 3 months"))
            .AddListContainer("Allocations ending soon with no future allocation:", endingPositionsObjectList)
            .AddListContainer("Personnel with more than 100% workload:", personnelMoreThan100PercentObjectList)
            .AddNewLine()
            .AddActionButton("Go to Personnel allocation app", personnelAllocationUri)
            .Build();


        return new SendNotificationsRequest()
        {
            Title = $"Weekly summary - {department.FullDepartmentName}",
            EmailPriority = 1,
            Card = card,
            Description = $"Weekly report for department - {department.FullDepartmentName}"
        };
    }

    private string PortalUri()
    {
        var portalUri = configuration["Endpoints_portal"] ?? "https://fusion.equinor.com/";
        if (!portalUri.EndsWith("/"))
            portalUri += "/";
        return portalUri;
    }

    public class AdaptiveCardBuilder
    {
        private readonly AdaptiveCard _adaptiveCard = new(new AdaptiveSchemaVersion(1, 0));

        public AdaptiveCardBuilder AddHeading(string text)
        {
            var heading = new AdaptiveTextBlock
            {
                Text = text,
                Wrap = true,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                Separator = true,
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            };
            _adaptiveCard.Body.Add(heading);
            return this;
        }

        public AdaptiveCardBuilder AddColumnSet(params AdaptiveCardColumn[] columns)
        {
            var columnSet = new AdaptiveColumnSet
            {
                Columns = columns.Select(col => col.Column).ToList(),
                Separator = true
            };
            _adaptiveCard.Body.Add(columnSet);
            return this;
        }

        public AdaptiveCardBuilder AddListContainer(string headerText, List<List<ListObject>> objectLists)
        {
            var listContainer = new AdaptiveContainer
            {
                Separator = true,
                Items = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Weight = AdaptiveTextWeight.Bolder,
                        Text = headerText,
                        Wrap = true,
                        Size = AdaptiveTextSize.Large
                    },
                    new AdaptiveColumnSet
                    {
                        Columns = new List<AdaptiveColumn>
                        {
                            new()
                            {
                                Width = AdaptiveColumnWidth.Stretch,
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveCardList(objectLists).List
                                }
                            }
                        }
                    }
                }
            };
            _adaptiveCard.Body.Add(listContainer);
            return this;
        }

        public AdaptiveCardBuilder AddActionButton(string title, string url)
        {
            var action = new AdaptiveOpenUrlAction()
            {
                Title = title,
                Url = new Uri(url)
            };

            _adaptiveCard.Actions.Add(action);

            return this;
        }

        public AdaptiveCardBuilder AddNewLine()
        {
            var container = new AdaptiveContainer()
            {
                Separator = true
            };

            _adaptiveCard.Body.Add(container);

            return this;
        }

        public AdaptiveCard Build()
        {
            return _adaptiveCard;
        }

        public class AdaptiveCardColumn
        {
            public AdaptiveColumn Column { get; }

            public AdaptiveCardColumn(string numberText, string headerText, string customText = "")
            {
                Column = new AdaptiveColumn
                {
                    Width = AdaptiveColumnWidth.Stretch,
                    Separator = true,
                    Spacing = AdaptiveSpacing.Medium,
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = $"{numberText} {customText}",
                            Wrap = true,
                            HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                            Size = AdaptiveTextSize.ExtraLarge
                        },
                        new AdaptiveTextBlock
                        {
                            Text = headerText,
                            Wrap = true,
                            HorizontalAlignment = AdaptiveHorizontalAlignment.Center
                        }
                    }
                };
            }
        }

        private class AdaptiveCardList
        {
            public AdaptiveContainer List { get; }

            public AdaptiveCardList(List<List<ListObject>> objectLists)
            {
                var listItems = new List<AdaptiveElement>();
                foreach (var objects in objectLists)
                {
                    var columns = new List<AdaptiveColumn>();
                    foreach (var o in objects)
                    {
                        var column = new AdaptiveColumn()
                        {
                            Width = AdaptiveColumnWidth.Stretch,
                            Items = new List<AdaptiveElement>
                            {
                                new AdaptiveTextBlock
                                {
                                    Text = $"{o.Value} ", Wrap = true,
                                    HorizontalAlignment = o.Alignment
                                }
                            }
                        };
                        columns.Add(column);
                    }

                    listItems.Add(new AdaptiveColumnSet()
                    {
                        Columns = columns
                    });
                }

                List = new AdaptiveContainer
                {
                    Items = listItems
                };
            }
        }
    }

    public class ListObject
    {
        public string Value { get; set; }
        public AdaptiveHorizontalAlignment Alignment { get; set; }
    }
}

