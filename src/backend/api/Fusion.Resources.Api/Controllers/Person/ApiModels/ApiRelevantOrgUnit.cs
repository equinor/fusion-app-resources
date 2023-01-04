using System.Collections.Generic;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers;

public class ApiRelevantOrgUnit
{
    public ApiRelevantOrgUnit(QueryRelevantOrgUnit queryModel)
    {
        SapId = queryModel.SapId;
        ShortName = queryModel.ShortName;
        Department = queryModel.Department;
        FullDepartment = queryModel.FullDepartment;
        Name = queryModel.Name;
        Reasons = queryModel.Reasons;
    }

    public string SapId { get; set; } = null!;

    public string ShortName { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string FullDepartment { get; set; } = null!;

    public string Name { get; set; } = null!;
    public List<string> Reasons { get; set; } = new();
}