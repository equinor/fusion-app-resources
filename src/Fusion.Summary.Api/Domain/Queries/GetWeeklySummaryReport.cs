using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Queries;

public class GetWeeklySummaryReport : IRequest<QueryWeeklySummaryReport?>
{
    public GetWeeklySummaryReport(string sapDepartmentId, Guid reportId)
    {
        ReportId = reportId;
        SapDepartmentId = sapDepartmentId;
    }

    public string SapDepartmentId { get; private set; }
    public Guid ReportId { get; private set; }

    public bool GetLatestReport { get; set; } = false;

    public static GetWeeklySummaryReport Latest(string sapDepartmentId)
    {
        return new GetWeeklySummaryReport(sapDepartmentId, Guid.Empty)
        {
            GetLatestReport = true
        };
    }


    public class Handler : IRequestHandler<GetWeeklySummaryReport, QueryWeeklySummaryReport?>
    {
        private readonly SummaryDbContext dbContext;

        public Handler(SummaryDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<QueryWeeklySummaryReport?> Handle(GetWeeklySummaryReport request, CancellationToken cancellationToken)
        {
            var query = dbContext.WeeklySummaryReports.Where(r => r.DepartmentSapId == request.SapDepartmentId);

            if (request.GetLatestReport)
            {
                var resolvedDate = GetPreviousWeeksMondayOrTodayDate(DateTime.UtcNow).Date;
                query = query.Where(r => r.Period == resolvedDate);
            }
            else
            {
                query = query.Where(r => r.Id == request.ReportId);
            }

            var dbReport = await query.FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (dbReport is null)
                return null;

            return QueryWeeklySummaryReport.FromDbSummaryReport(dbReport);
        }

        private static DateTime GetPreviousWeeksMondayOrTodayDate(DateTime date)
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