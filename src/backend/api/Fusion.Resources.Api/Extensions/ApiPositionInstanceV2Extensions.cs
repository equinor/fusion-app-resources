using System;
using Fusion.ApiClients.Org;

namespace Fusion.Resources.Api
{
    public static class ApiPositionInstanceV2Extensions
    {
        public static string GetFormattedPeriodString(this ApiPositionInstanceV2 instance)
        {
            var months = GetMonths(instance);
            return $"🗓{instance.AppliesFrom:dd/MM/yy} to 🗓{instance.AppliesTo:dd/MM/yy} ({months} mths)";
        }
        public static string GetFormattedWorkloadString(this ApiPositionInstanceV2 instance)
        {
            if (!instance.Workload.HasValue)
                return string.Empty;


            return $"{instance.Workload.Value:F0}% ({(instance.Workload.Value / 100):F2} FTE)";
        }

        private static int GetMonths(ApiPositionInstanceV2 instance)
        {
            int monthsApart = 12 * (instance.AppliesFrom.Year - instance.AppliesTo.Year) + instance.AppliesFrom.Month - instance.AppliesTo.Month;
            return Math.Abs(monthsApart);
        }
    }
}