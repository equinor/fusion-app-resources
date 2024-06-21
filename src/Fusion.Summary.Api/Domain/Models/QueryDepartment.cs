using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Models;

namespace Fusion.Summary.Api.Domain.Models;

public class QueryDepartment
{
    public string SapDepartmentId { get; set; } = string.Empty;
    public Guid ResourceOwnerAzureUniqueId { get; set; }
    public string FullDepartmentName { get; set; } = string.Empty;

    public static QueryDepartment FromDbDepartment(DbDepartment dbDepartment)
    {
        return new QueryDepartment
        {
            SapDepartmentId = dbDepartment.DepartmentSapId,
            ResourceOwnerAzureUniqueId = dbDepartment.ResourceOwnerAzureUniqueId,
            FullDepartmentName = dbDepartment.FullDepartmentName
        };
    }

    public static QueryDepartment FromApiDepartment(ApiDepartment apiDepartment)
    {
        return new QueryDepartment
        {
            SapDepartmentId = apiDepartment.DepartmentSapId,
            FullDepartmentName = apiDepartment.FullDepartmentName
        };
    }
}
