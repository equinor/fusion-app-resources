using Fusion.AspNetCore.OData;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Domain.Models;
using Fusion.Summary.Api.Domain.Queries.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Queries;

public class GetWeeklySummaryReports : IRequest<QueryCollection<QueryWeeklySummaryReport>>
{
    public GetWeeklySummaryReports(string sapDepartmentId, ODataQueryParams query)
    {
        Query = query;
        SapDepartmentId = sapDepartmentId;
    }

    public string SapDepartmentId { get; private set; }
    public ODataQueryParams Query { get; private set; }


    public class Handler : IRequestHandler<GetWeeklySummaryReports, QueryCollection<QueryWeeklySummaryReport>>
    {
        private readonly SummaryDbContext _dbcontext;

        public Handler(SummaryDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        public async Task<QueryCollection<QueryWeeklySummaryReport>> Handle(GetWeeklySummaryReports request,
            CancellationToken cancellationToken)
        {
            var getReportQuery = _dbcontext.WeeklySummaryReports
                .Where(r => r.DepartmentSapId == request.SapDepartmentId)
                .AsSplitQuery();

            if (request.Query.HasFilter)
            {
                getReportQuery = getReportQuery.ApplyODataFilters(request.Query,
                    m => { m.MapField(nameof(ApiWeeklySummaryReport.Period), r => r.Period); });
            }

            getReportQuery = getReportQuery.ApplyODataSorting(request.Query, m =>
            {
                m.MapField(nameof(ApiWeeklySummaryReport.Id), r => r.Id);
                m.MapField(nameof(ApiWeeklySummaryReport.Period), r => r.Period);
            }, q => q.OrderByDescending(p => p.Period).ThenBy(p => p.Id));

            var totalCount = await getReportQuery.CountAsync(cancellationToken: cancellationToken);

            var skip = request.Query.Skip.GetValueOrDefault(0);
            var top = request.Query.Top.GetValueOrDefault(10);
            var reports = await getReportQuery
                .Skip(skip)
                .Take(top)
                .ToListAsync(cancellationToken: cancellationToken);


            return new QueryCollection<QueryWeeklySummaryReport>(
                reports.Select(QueryWeeklySummaryReport.FromDbSummaryReport))
            {
                Skip = skip,
                Top = top,
                TotalCount = totalCount
            };
        }
    }
}