using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Commands;

public class SetSummaryReport
{
    public string SapDepartmentId { get; private set; }

    // Using api model to reduce repetitive code
    public PutSummaryReportRequest SummaryReport { get; private set; }

    public SetSummaryReport(string sapDepartmentId, PutSummaryReportRequest summaryReport)
    {
        SapDepartmentId = sapDepartmentId;
        SummaryReport = summaryReport;
    }


    public class Handler
    {
        private readonly DbContext _context;

        public Handler(DbContext context)
        {
            _context = context;
        }

        public async Task Handle(SetSummaryReport request, CancellationToken cancellationToken)
        {
            // SapDepartmentId exists check
            // TODO:

            DbSet<DbSummaryReport> dbSet = null!;


            // As this is a put operation, replace existing one if it exists
            var existingReport = await dbSet.FirstOrDefaultAsync(r => r.DepartmentSapId == request.SapDepartmentId &&
                                                                      r.PeriodType.ToString() ==
                                                                      request.SummaryReport.PeriodType.ToString() &&
                                                                      r.Period.Date == request.SummaryReport.Period
                                                                          .Date, cancellationToken: cancellationToken);

            if (existingReport is not null)
                dbSet.Remove(existingReport);

            var dbSummaryReport = new DbSummaryReport()
            {
                Id = existingReport?.Id ?? Guid.NewGuid(),
                DepartmentSapId = request.SapDepartmentId,
                PeriodType = Enum.Parse<DbSummaryReportPeriod>(request.SummaryReport.PeriodType.ToString()),
                Period = request.SummaryReport.Period.Date,
                NumberOfPersonnel = request.SummaryReport.NumberOfPersonnel,
                CapacityInUse = request.SummaryReport.CapacityInUse,
                NumberOfRequestsLastPeriod = request.SummaryReport.NumberOfRequestsLastPeriod,
                NumberOfOpenRequests = request.SummaryReport.NumberOfOpenRequests,
                NumberOfRequestsStartingInLessThanThreeMonths =
                    request.SummaryReport.NumberOfRequestsStartingInLessThanThreeMonths,
                NumberOfRequestsStartingInMoreThanThreeMonths =
                    request.SummaryReport.NumberOfRequestsStartingInMoreThanThreeMonths,
                AverageTimeToHandleRequests = request.SummaryReport.AverageTimeToHandleRequests,
                AllocationChangesAwaitingTaskOwnerAction =
                    request.SummaryReport.AllocationChangesAwaitingTaskOwnerAction,
                ProjectChangesAffectingNextThreeMonths = request.SummaryReport.ProjectChangesAffectingNextThreeMonths,
                PositionsEnding = request.SummaryReport.PositionsEnding
                    .Select(pe => new DbEndingPosition()
                    {
                        Id = Guid.NewGuid(),
                        FullName = pe.FullName,
                        EndDate = pe.EndDate
                    })
                    .ToList(),
                PersonnelMoreThan100PercentFTE = request.SummaryReport.PersonnelMoreThan100PercentFTE
                    .Select(pm => new DbPersonnelMoreThan100PercentFTE()
                    {
                        Id = Guid.NewGuid(),
                        FullName = pm.FullName,
                        FTE = pm.FTE
                    })
                    .ToList()
            };

            // TODO:
            dbSet.Add(dbSummaryReport);

            // TODO:
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}