using System.Collections.Generic;
using System;

namespace Fusion.Resources.Domain.Models
{
    internal class QueryOrgUnitReason
    {
        public QueryOrgUnitReason(string fullDepartment, string reason)
        {
            IsWildCard = fullDepartment.Trim().EndsWith('*');
            Reason = reason;
            FullDepartment = fullDepartment.Replace("*", "").Trim();
            Level = FullDepartment.Split(" ").Length;
            IsGlobalRole = string.IsNullOrEmpty(fullDepartment);

        }

        public string FullDepartment { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public bool IsWildCard { get; set; }
        public int Level { get; set; }
        public bool IsGlobalRole { get; set; }
    }

    internal struct OrgUnitComparer
    {
        public OrgUnitComparer(string FullDepartment)
        {
            this.FullDepartment = FullDepartment;
            Level = FullDepartment.Split(" ").Length;
        }

        public string FullDepartment { get; }
        public int Level { get; set; }

        public bool IsChildOf(OrgUnitComparer other, int maxDistance)
        {
            var isChild = other.FullDepartment.StartsWith(FullDepartment + " ", StringComparison.OrdinalIgnoreCase);
            var distance = Level - other.Level;

            return isChild && distance <= maxDistance;
        }
    }
}
