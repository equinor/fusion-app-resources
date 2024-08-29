using Fusion.Summary.Api.Domain.Models;

namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiDepartment
{
    public string DepartmentSapId { get; set; } = string.Empty;
    public string FullDepartmentName { get; set; } = string.Empty;

    public Guid[] ResourceOwnersAzureUniqueId { get; init; } = null!;

    public Guid[] DelegateResourceOwnersAzureUniqueId { get; init; } = null!;

    public static ApiDepartment FromQueryDepartment(QueryDepartment queryDepartment)
    {
        return new ApiDepartment
        {
            DepartmentSapId = queryDepartment.SapDepartmentId,
            FullDepartmentName = queryDepartment.FullDepartmentName,
            ResourceOwnersAzureUniqueId = queryDepartment.ResourceOwnersAzureUniqueId.ToArray(),
            DelegateResourceOwnersAzureUniqueId = queryDepartment.DelegateResourceOwnersAzureUniqueId.ToArray()
        };
    }
}