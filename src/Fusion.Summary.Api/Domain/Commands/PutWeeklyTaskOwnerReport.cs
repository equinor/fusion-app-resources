using Fusion.Summary.Api.Controllers.Requests;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Commands;

public class PutWeeklyTaskOwnerReport : IRequest<QueryWeeklyTaskOwnerReport>
{
    public Guid ProjectId { get; }
    public PutWeeklyTaskOwnerReportRequest Report { get; }

    public PutWeeklyTaskOwnerReport(Guid projectId, PutWeeklyTaskOwnerReportRequest report)
    {
        ProjectId = projectId;
        Report = report;
    }


    public class Handler : IRequestHandler<PutWeeklyTaskOwnerReport, QueryWeeklyTaskOwnerReport>
    {
        private readonly SummaryDbContext _dbContext;

        public Handler(SummaryDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<QueryWeeklyTaskOwnerReport> Handle(PutWeeklyTaskOwnerReport request, CancellationToken cancellationToken)
        {
            var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);
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
                ProjectId = project.Id,
                ActionsAwaitingTaskOwnerAction = request.Report.ActionsAwaitingTaskOwnerAction,
                AdminAccessExpiringInLessThanThreeMonths = request.Report.AdminAccessExpiringInLessThanThreeMonths.Select(x => new DbAdminAccessExpiring()
                {
                    AzureUniqueId = x.AzureUniqueId,
                    FullName = x.FullName,
                    Expires = x.Expires
                }).ToList(),
                PositionAllocationsEndingInNextThreeMonths = request.Report.PositionAllocationsEndingInNextThreeMonths.Select(x => new DbPositionAllocationEnding()
                {
                    PositionExternalId = x.PositionExternalId,
                    PositionName = x.PositionName,
                    PositionAppliesTo = x.PositionAppliesTo,
                    PositionNameDetailed = x.PositionNameDetailed
                }).ToList(),
                TBNPositionsStartingInLessThanThreeMonths = request.Report.TBNPositionsStartingInLessThanThreeMonths.Select(x => new DbTBNPositionStartingSoon()
                {
                    PositionExternalId = x.PositionExternalId,
                    PositionName = x.PositionName,
                    PositionAppliesFrom = x.PositionAppliesFrom,
                    PositionNameDetailed = x.PositionNameDetailed
                }).ToList()

            };


            _dbContext.WeeklyTaskOwnerReports.Add(report);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return QueryWeeklyTaskOwnerReport.FromDbWeeklyTaskOwnerReport(report);
        }
    }
}