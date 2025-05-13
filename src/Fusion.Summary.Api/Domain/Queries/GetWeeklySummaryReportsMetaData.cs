using Fusion.AspNetCore.OData;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Domain.Models;
using Fusion.Summary.Api.Domain.Queries.Base;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Domain.Queries;

public class GetWeeklySummaryReportsMetaData : IRequest<QueryCollection<QueryReportMetaData>>
{
    public GetWeeklySummaryReportsMetaData(string sapDepartmentId, ODataQueryParams query)
    {
        Query = query;
        SapDepartmentId = sapDepartmentId;
    }

    public string SapDepartmentId { get; private set; }
    public ODataQueryParams Query { get; private set; }


    public class Handler : IRequestHandler<GetWeeklySummaryReportsMetaData, QueryCollection<QueryReportMetaData>>
    {
        private readonly SummaryDbContext dbContext;

        public Handler(SummaryDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<QueryCollection<QueryReportMetaData>> Handle(GetWeeklySummaryReportsMetaData request, CancellationToken cancellationToken)
        {
            var getReportQuery = dbContext.WeeklySummaryReports
                .Where(r => r.DepartmentSapId == request.SapDepartmentId)
                .OrderByDescending(r => r.Period).ThenBy(r => r.Id)
                .Select(r => new { r.Id, r.Period });


            var totalCount = await getReportQuery.CountAsync(cancellationToken: cancellationToken);

            if (request.Query.HasFilter)
            {
                getReportQuery = getReportQuery.ApplyODataFilters(request.Query,
                    m => { m.MapField(nameof(QueryReportMetaData.Period), r => r.Period); });
            }

            getReportQuery = getReportQuery.ApplyODataSorting(request.Query, m =>
            {
                m.MapField(nameof(QueryReportMetaData.Period), r => r.Period);
                m.MapField(nameof(QueryReportMetaData.Id), r => r.Id);
            }, q => q.OrderByDescending(p => p.Period).ThenBy(p => p.Id));


            var skip = request.Query.Skip.GetValueOrDefault(0);
            var top = request.Query.Top.GetValueOrDefault(10);
            var reports = await getReportQuery
                .Skip(skip)
                .Take(top)
                .ToListAsync(cancellationToken: cancellationToken);

            var reportMetaData = reports.Select(r => new QueryReportMetaData(r.Id, r.Period));

            return new QueryCollection<QueryReportMetaData>(reportMetaData)
            {
                Skip = skip,
                Top = top,
                TotalCount = totalCount
            };
        }
    }
}