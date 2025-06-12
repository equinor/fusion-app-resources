using System;
using Fusion.Resources.Domain.Models;
using System.Linq;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRelevantOrgUnit
    {
        public ApiRelevantOrgUnit(QueryRelevantOrgUnit relevantOrgUnit)
        {
            Reasons = relevantOrgUnit.Reasons.ToArray();
            Name = relevantOrgUnit.Name;
            SapId = relevantOrgUnit.SapId;
            ParentSapId = relevantOrgUnit.ParentSapId;
            ShortName = relevantOrgUnit.ShortName;
            FullDepartment = relevantOrgUnit.FullDepartment;
            Department = relevantOrgUnit.Department;
        }

        public string? Name { get; set; }
        public string? SapId { get; set; }
        public string? ParentSapId { get; set; }
        public string? ShortName { get; set; }
        public string? Department { get; set; }
        public string? FullDepartment { get; set; }
        public string[] Reasons { get; set; }
    }
}
