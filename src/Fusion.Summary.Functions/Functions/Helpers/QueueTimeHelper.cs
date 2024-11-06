using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Fusion.Summary.Functions.Functions.Helpers;

public static class QueueTimeHelper
{
    /// <summary>
    ///     Calculate the enqueue time for each value based on the total batch time and amount of values. This should
    ///     spread the work over the total batch time.
    /// </summary>
    public static Dictionary<T, DateTimeOffset> CalculateEnqueueTime<T>(List<T> values, TimeSpan totalBatchTime, ILogger? logger = null) where T : notnull
    {
        var currentTime = DateTimeOffset.UtcNow;
        var minutesPerReportSlice = totalBatchTime.TotalMinutes / values.Count;

        logger?.LogInformation("Minutes allocated for each worker: {MinutesPerReportSlice}", minutesPerReportSlice);

        var delayMapping = new Dictionary<T, DateTimeOffset>();
        foreach (var value in values)
        {
            // First values has no delay
            if (delayMapping.Count == 0)
            {
                delayMapping.Add(value, currentTime);
                continue;
            }

            var enqueueTime = delayMapping.Last().Value.AddMinutes(minutesPerReportSlice);
            delayMapping.Add(value, enqueueTime);
        }

        return delayMapping;
    }
}