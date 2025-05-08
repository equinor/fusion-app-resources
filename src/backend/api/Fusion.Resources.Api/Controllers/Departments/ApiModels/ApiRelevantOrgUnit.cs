using System;
using Fusion.Resources.Domain.Models;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRelevantOrgUnit
    {
        public ApiRelevantOrgUnit(QueryRelevantOrgUnit relevantOrgUnit)
        {
            Reasons = relevantOrgUnit.Reasons.Select(reason => Enum.Parse<ApiRelevantOrgUnitReasons>(reason, true)).ToArray();
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
        public ApiRelevantOrgUnitReasons[] Reasons { get; set; }
    }

    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public enum ApiRelevantOrgUnitReasons
    {
        Manager,
        ParentManager,
        SiblingManager,
        DelegatedManager,
        DelegatedParentManager,
        DelegatedSiblingManager
    }
}
