using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Commands;

public class PutWeeklyTaskOwnerReport : IRequest<bool>
{
    public Guid ProjectId { get; }
    public PutWeeklyTaskOwnerReportRequest Report { get; }

    public PutWeeklyTaskOwnerReport(Guid projectId, PutWeeklyTaskOwnerReportRequest report)
    {
        ProjectId = projectId;
        Report = report;
    }


    public class Handler : IRequestHandler<PutWeeklyTaskOwnerReport, bool>
    {
        private readonly SummaryDbContext _dbContext;

        public Handler(SummaryDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Handle(PutWeeklyTaskOwnerReport request, CancellationToken cancellationToken)
        {
            var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId || p.OrgProjectExternalId == request.ProjectId, cancellationToken);
            if (project == null)
                throw new InvalidOperationException($"Project with id '{request.ProjectId}' was not found");

            var existingReport = await _dbContext.WeeklyTaskOwnerReports
                .FirstOrDefaultAsync(r => r.ProjectId == project.Id &&
                                          request.Report.PeriodStart.Date == r.PeriodStart.Date &&
                                          request.Report.PeriodEnd.Date == r.PeriodEnd.Date, cancellationToken);

            if (existingReport is not null)
                _dbContext.WeeklyTaskOwnerReports.Remove(existingReport);

            var report = new DbWeeklyTaskOwnerReport()
            {
                Id = existingReport?.Id ?? Guid.NewGuid(),
                PeriodStart = request.Report.PeriodStart,
                PeriodEnd = request.Report.PeriodEnd,
                ProjectId = project.Id
            };


            _dbContext.WeeklyTaskOwnerReports.Add(report);

            await _dbContext.SaveChangesAsync(cancellationToken);

            // return true if a new report was created
            return existingReport is null;
        }
    }
}