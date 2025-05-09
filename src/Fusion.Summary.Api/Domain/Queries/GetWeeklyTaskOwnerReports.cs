﻿using Fusion.AspNetCore.OData;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Domain.Models;
using Fusion.Summary.Api.Domain.Queries.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Queries;

public class GetWeeklyTaskOwnerReports : IRequest<QueryCollection<QueryWeeklyTaskOwnerReport>>
{
    public ODataQueryParams Query { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? ReportId { get; private set; }

    public GetWeeklyTaskOwnerReports(ODataQueryParams? query = null)
    {
        Query = query ?? new ODataQueryParams();
    }

    public GetWeeklyTaskOwnerReports WhereProjectId(Guid projectId)
    {
        ProjectId = projectId;
        return this;
    }

    public GetWeeklyTaskOwnerReports WhereReportId(Guid reportId)
    {
        ReportId = reportId;
        return this;
    }

    public class Handler : IRequestHandler<GetWeeklyTaskOwnerReports, QueryCollection<QueryWeeklyTaskOwnerReport>>
    {
        private readonly SummaryDbContext _dbContext;

        public Handler(SummaryDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<QueryCollection<QueryWeeklyTaskOwnerReport>> Handle(GetWeeklyTaskOwnerReports request, CancellationToken cancellationToken)
        {
            var query = _dbContext.WeeklyTaskOwnerReports
                .AsQueryable();

            if (request.ProjectId.HasValue)
                query = query.Where(x => x.ProjectId == request.ProjectId);

            if (request.ReportId.HasValue)
                query = query.Where(x => x.Id == request.ReportId);

            query = query.OrderByDescending(r => r.PeriodStart)
                .ThenBy(r => r.Id);

            if (request.Query.HasFilter)
            {
                query = query.ApplyODataFilters(request.Query,
                    m =>
                    {
                        m.MapField(nameof(ApiWeeklyTaskOwnerReport.PeriodStart), r => r.PeriodStart);
                        m.MapField(nameof(ApiWeeklyTaskOwnerReport.PeriodEnd), r => r.PeriodEnd);
                    });
            }

            var totalCount = await query.CountAsync(cancellationToken: cancellationToken);

            var skip = request.Query.Skip.GetValueOrDefault(0);
            var top = request.Query.Top.GetValueOrDefault(10);
            var reports = await query
                .Skip(skip)
                .Take(top)
                .ToListAsync(cancellationToken: cancellationToken);

            return new QueryCollection<QueryWeeklyTaskOwnerReport>(reports.Select(QueryWeeklyTaskOwnerReport.FromDbWeeklyTaskOwnerReport), top, skip, totalCount);
        }
    }
}