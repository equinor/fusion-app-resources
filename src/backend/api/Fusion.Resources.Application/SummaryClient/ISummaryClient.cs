using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Application.SummaryClient.Models;

namespace Fusion.Resources.Application.SummaryClient;

public interface ISummaryClient
{
    public Task<SummaryApiCollectionDto<ResourceOwnerWeeklySummaryReportDto>> GetSummaryReportForPeriodStartAsync(string departmentSapId, DateTime periodStart, CancellationToken cancellationToken = default);
    public Task<SummaryApiCollectionDto<ResourceOwnerWeeklySummaryReportDto>> GetSummaryReportsAsync(string departmentSapId, int? top, int? skip, CancellationToken cancellationToken = default);
}