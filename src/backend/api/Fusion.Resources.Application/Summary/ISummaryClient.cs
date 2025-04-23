using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Application.Summary.Models;

namespace Fusion.Resources.Application.Summary;

public interface ISummaryClient
{
    public Task<ResourceOwnerWeeklySummaryReport?> GetSummaryReportForPeriodStartAsync(string departmentSapId, DateTime periodStart, CancellationToken cancellationToken = default);
}