using Fusion.AspNetCore.OData;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Domain.Models;
using Fusion.Summary.Api.Domain.Queries.Base;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Queries;

public class GetSummaryReport
{
    public GetSummaryReport(string sapDepartmentId, ODataQueryParams query)
    {
        Query = query;
        SapDepartmentId = sapDepartmentId;
    }

    public string SapDepartmentId { get; private set; }
    public ODataQueryParams Query { get; private set; }


    public class Handler
    {
        public async Task<QueryCollection<QuerySummaryReport>> Handle(GetSummaryReport request,
            CancellationToken cancellationToken)
        {
            // TODO:
            DbSet<DbSummaryReport> dbSet = null!;

            var getReportQuery = dbSet.Where(r => r.DepartmentSapId == request.SapDepartmentId);

            if (request.Query.HasFilter)
            {
                getReportQuery = getReportQuery.ApplyODataFilters(request.Query, m =>
                {
                    m.MapField(nameof(ApiSummaryReport.PeriodType), r => r.PeriodType);
                    m.MapField(nameof(ApiSummaryReport.Period), r => r.Period);
                    m.MapField(nameof(ApiSummaryReport.PersonnelMoreThan100PercentFTE),
                        r => r.PersonnelMoreThan100PercentFTE);
                    m.MapField(nameof(ApiSummaryReport.PositionsEnding), r => r.PositionsEnding);
                });
            }

            getReportQuery = getReportQuery.ApplyODataSorting(request.Query, m =>
            {
                m.MapField(nameof(ApiSummaryReport.Id), r => r.Id);
                m.MapField(nameof(ApiSummaryReport.Period), r => r.Period);
            }, q => q.OrderBy(p => p.Period).ThenBy(p => p.Id));

            var totalCount = await getReportQuery.CountAsync(cancellationToken: cancellationToken);

            var skip = request.Query.Skip.GetValueOrDefault(0);
            var top = request.Query.Top.GetValueOrDefault(10);
            var reports = await getReportQuery
                .Skip(skip)
                .Take(top)
                .ToListAsync(cancellationToken: cancellationToken);


            return new QueryCollection<QuerySummaryReport>(reports.Select(QuerySummaryReport.FromDbSummaryReport))
            {
                Skip = skip,
                Top = top,
                TotalCount = totalCount
            };
        }
    }
}