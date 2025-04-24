using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Application.SummaryClient;
using Fusion.Resources.Application.SummaryClient.Models;
using MediatR;

namespace Fusion.Resources.Domain.Queries;

public class GetSummaryReports : IRequest<SummaryApiCollectionDto<ResourceOwnerWeeklySummaryReportDto>>
{
    public required string DepartmentSapId { get; init; }

    public DateTime? PeriodStart { get; private init; }

    public int? Top { get; private init; }
    public int? Skip { get; private init; }

    private GetSummaryReports()
    {
    }


    public static GetSummaryReports ForPeriodStart(string departmentSapId, DateTime periodStart)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentSapId);

        return new GetSummaryReports
        {
            PeriodStart = periodStart,
            DepartmentSapId = departmentSapId
        };
    }

    public static GetSummaryReports GetWithTopAndSkip(string departmentSapId, int? top, int? skip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentSapId);

        return new GetSummaryReports
        {
            Top = top,
            Skip = skip,
            DepartmentSapId = departmentSapId
        };
    }

    public class Handler : IRequestHandler<GetSummaryReports, SummaryApiCollectionDto<ResourceOwnerWeeklySummaryReportDto>>
    {
        private readonly ISummaryClient summaryClient;

        public Handler(ISummaryClient summaryClient)
        {
            this.summaryClient = summaryClient;
        }

        public async Task<SummaryApiCollectionDto<ResourceOwnerWeeklySummaryReportDto>> Handle(GetSummaryReports request, CancellationToken cancellationToken)
        {
            if (request.PeriodStart != null)
            {
                var resolvedDate = GetPreviousWeeksMondayOrTodayDate(request.PeriodStart.Value);

                return await summaryClient.GetSummaryReportForPeriodStartAsync(request.DepartmentSapId, resolvedDate, cancellationToken);
            }

            return await summaryClient.GetSummaryReportsAsync(request.DepartmentSapId, request.Top, request.Skip, cancellationToken);
        }


        private static DateTime GetPreviousWeeksMondayOrTodayDate(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    return date.AddDays(-6);
                case DayOfWeek.Monday:
                    return date;
                case DayOfWeek.Tuesday:
                case DayOfWeek.Wednesday:
                case DayOfWeek.Thursday:
                case DayOfWeek.Friday:
                case DayOfWeek.Saturday:
                default:
                {
                    // Calculate days until previous monday
                    // Go one week back and then remove the days until the monday
                    var daysUntilLastWeeksMonday = 1 - (int)date.DayOfWeek - 7;

                    return date.AddDays(daysUntilLastWeeksMonday);
                }
            }
        }
    }
}