using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Application.SummaryClient;
using Fusion.Resources.Application.SummaryClient.Models;
using MediatR;

namespace Fusion.Resources.Domain.Queries;

public class GetSummaryReport : IRequest<ResourceOwnerWeeklySummaryReportDto?>
{
    public required string DepartmentSapId { get; init; }

    [MemberNotNullWhen(false, nameof(PeriodStart))]
    public bool GetLatest { get; private init; }

    public DateTime? PeriodStart { get; private init; }

    private GetSummaryReport()
    {
    }

    public static GetSummaryReport Latest(string departmentSapId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentSapId);

        return new GetSummaryReport
        {
            GetLatest = true,
            DepartmentSapId = departmentSapId
        };
    }

    public static GetSummaryReport ForPeriodStart(string departmentSapId, DateTime periodStart)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentSapId);

        return new GetSummaryReport
        {
            GetLatest = false,
            PeriodStart = periodStart,
            DepartmentSapId = departmentSapId
        };
    }

    public class Handler : IRequestHandler<GetSummaryReport, ResourceOwnerWeeklySummaryReportDto?>
    {
        private readonly ISummaryClient summaryClient;

        public Handler(ISummaryClient summaryClient)
        {
            this.summaryClient = summaryClient;
        }

        public async Task<ResourceOwnerWeeklySummaryReportDto?> Handle(GetSummaryReport request, CancellationToken cancellationToken)
        {
            if (request.GetLatest)
            {
                var lastWeekMondayDate = GetPreviousWeeksMondayDate(DateTime.UtcNow.Date);
                return await summaryClient.GetSummaryReportForPeriodStartAsync(request.DepartmentSapId, lastWeekMondayDate, cancellationToken);
            }

            return await summaryClient.GetSummaryReportForPeriodStartAsync(request.DepartmentSapId, request.PeriodStart.Value, cancellationToken);
        }


        private static DateTime GetPreviousWeeksMondayDate(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    return date.AddDays(-6);
                case DayOfWeek.Monday:
                    return date.AddDays(-7);
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