using Fusion.AspNetCore.OData;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Domain.Models;
using Fusion.Summary.Api.Domain.Queries.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Queries;

public class GetSummaryReports : IRequest<QueryCollection<QuerySummaryReport>>
{
    public GetSummaryReports(string sapDepartmentId, ODataQueryParams query)
    {
        Query = query;
        SapDepartmentId = sapDepartmentId;
    }

    public string SapDepartmentId { get; private set; }
    public ODataQueryParams Query { get; private set; }


    public class Handler : IRequestHandler<GetSummaryReports, QueryCollection<QuerySummaryReport>>
    {
        private readonly SummaryDbContext _dbcontext;

        public Handler(SummaryDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        public async Task<QueryCollection<QuerySummaryReport>> Handle(GetSummaryReports request,
            CancellationToken cancellationToken)
        {
            var getReportQuery = _dbcontext.SummaryReports.Where(r => r.DepartmentSapId == request.SapDepartmentId);

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
            }, q => q.OrderByDescending(p => p.Period).ThenBy(p => p.Id));

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