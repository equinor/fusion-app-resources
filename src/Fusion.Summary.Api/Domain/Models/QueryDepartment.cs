using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Summary.Api.Database.Models;

namespace Fusion.Summary.Api.Domain.Models;

public class QueryDepartment
{
    public string SapDepartmentId { get; set; } = string.Empty;
    public string FullDepartmentName { get; set; } = string.Empty;
    public List<Guid> ResourceOwnersAzureUniqueId { get; set; } = null!;

    public List<Guid> DelegateResourceOwnersAzureUniqueId { get; set; } = null!;

    public static QueryDepartment FromDbDepartment(DbDepartment dbDepartment)
    {
        return new QueryDepartment
        {
            SapDepartmentId = dbDepartment.DepartmentSapId,
            FullDepartmentName = dbDepartment.FullDepartmentName,
            ResourceOwnersAzureUniqueId = dbDepartment.ResourceOwnersAzureUniqueId.ToList(),
            DelegateResourceOwnersAzureUniqueId = dbDepartment.DelegateResourceOwnersAzureUniqueId.ToList()
        };
    }

    public static QueryDepartment FromApiDepartment(ApiDepartment apiDepartment)
    {
        return new QueryDepartment
        {
            SapDepartmentId = apiDepartment.DepartmentSapId,
            FullDepartmentName = apiDepartment.FullDepartmentName,
            ResourceOwnersAzureUniqueId = apiDepartment.ResourceOwnersAzureUniqueId.ToList(),
            DelegateResourceOwnersAzureUniqueId = apiDepartment.DelegateResourceOwnersAzureUniqueId.ToList()
        };
    }
}
