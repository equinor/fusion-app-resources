using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Commands;

public class PutSummaryReport : IRequest
{
    public string SapDepartmentId { get; private set; }

    // Using api model to reduce repetitive code
    public PutSummaryReportRequest SummaryReport { get; private set; }

    public PutSummaryReport(string sapDepartmentId, PutSummaryReportRequest summaryReport)
    {
        SapDepartmentId = sapDepartmentId;
        SummaryReport = summaryReport;
    }


    public class Handler : IRequestHandler<PutSummaryReport>
    {
        private readonly DatabaseContext _dbContext;

        public Handler(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(PutSummaryReport request, CancellationToken cancellationToken)
        {
            if (await _dbContext.Departments.AnyAsync(d => d.DepartmentSapId == request.SapDepartmentId,
                    cancellationToken: cancellationToken))
                throw new InvalidOperationException("Department does not exist");


            // As this is a put operation, replace existing one if it exists
            var existingReport = await _dbContext.SummaryReports.FirstOrDefaultAsync(r =>
                r.DepartmentSapId == request.SapDepartmentId &&
                r.PeriodType.ToString() ==
                request.SummaryReport.PeriodType.ToString() &&
                r.Period.Date == request.SummaryReport.Period
                    .Date, cancellationToken: cancellationToken);


            if (existingReport is not null)
                _dbContext.SummaryReports.Remove(existingReport);

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

            _dbContext.SummaryReports.Add(dbSummaryReport);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}