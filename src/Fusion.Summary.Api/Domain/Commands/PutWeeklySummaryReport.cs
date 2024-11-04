using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Commands;

public class PutWeeklySummaryReport : IRequest<bool>
{
    public string SapDepartmentId { get; private set; }

    // Using api model to reduce repetitive code
    public PutWeeklySummaryReportRequest WeeklySummaryReport { get; private set; }

    public PutWeeklySummaryReport(string sapDepartmentId, PutWeeklySummaryReportRequest weeklySummaryReport)
    {
        SapDepartmentId = sapDepartmentId;
        WeeklySummaryReport = weeklySummaryReport;
    }


    public class Handler : IRequestHandler<PutWeeklySummaryReport, bool>
    {
        private readonly SummaryDbContext _dbContext;

        public Handler(SummaryDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(PutWeeklySummaryReport request, CancellationToken cancellationToken)
        {
            if (!await _dbContext.Departments.AnyAsync(d => d.DepartmentSapId == request.SapDepartmentId,
                    cancellationToken: cancellationToken))
                throw new InvalidOperationException("Department does not exist");


            // As this is a put operation, replace existing one if it exists
            var existingReport = await _dbContext.WeeklySummaryReports.FirstOrDefaultAsync(r =>
                r.DepartmentSapId == request.SapDepartmentId &&
                r.Period.Date == request.WeeklySummaryReport.Period.Date,
                cancellationToken: cancellationToken);


            if (existingReport is not null)
                _dbContext.WeeklySummaryReports.Remove(existingReport);

            var dbSummaryReport = new DbWeeklySummaryReport()
            {
                Id = existingReport?.Id ?? Guid.NewGuid(),
                DepartmentSapId = request.SapDepartmentId,
                Period = request.WeeklySummaryReport.Period.Date,
                NumberOfPersonnel = request.WeeklySummaryReport.NumberOfPersonnel,
                CapacityInUse = request.WeeklySummaryReport.CapacityInUse,
                NumberOfRequestsLastPeriod = request.WeeklySummaryReport.NumberOfRequestsLastPeriod,
                NumberOfOpenRequests = request.WeeklySummaryReport.NumberOfOpenRequests,
                NumberOfRequestsStartingInLessThanThreeMonths =
                    request.WeeklySummaryReport.NumberOfRequestsStartingInLessThanThreeMonths,
                NumberOfRequestsStartingInMoreThanThreeMonths =
                    request.WeeklySummaryReport.NumberOfRequestsStartingInMoreThanThreeMonths,
                AverageTimeToHandleRequests = request.WeeklySummaryReport.AverageTimeToHandleRequests,
                AllocationChangesAwaitingTaskOwnerAction =
                    request.WeeklySummaryReport.AllocationChangesAwaitingTaskOwnerAction,
                ProjectChangesAffectingNextThreeMonths =
                    request.WeeklySummaryReport.ProjectChangesAffectingNextThreeMonths,
                PositionsEnding = request.WeeklySummaryReport.PositionsEnding
                    .Select(pe => new DbEndingPosition()
                    {
                        FullName = pe.FullName,
                        EndDate = pe.EndDate
                    })
                    .ToList(),
                PersonnelMoreThan100PercentFTE = request.WeeklySummaryReport.PersonnelMoreThan100PercentFTE
                    .Select(pm => new DbPersonnelMoreThan100PercentFTE()
                    {
                        FullName = pm.FullName,
                        FTE = pm.FTE
                    })
                    .ToList()
            };

            _dbContext.WeeklySummaryReports.Add(dbSummaryReport);

            await _dbContext.SaveChangesAsync(cancellationToken);

            // return true if a new report was created
            return existingReport is null;
        }
    }
}