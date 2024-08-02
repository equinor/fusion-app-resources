using Fusion.Summary.Api.Domain.Models;

namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiDepartment
{
    public string DepartmentSapId { get; set; } = string.Empty;
    public Guid ResourceOwnerAzureUniqueId { get; set; }
    public string FullDepartmentName { get; set; } = string.Empty;

    public static ApiDepartment FromQueryDepartment(QueryDepartment queryDepartment)
    {
        return new ApiDepartment
        {
            DepartmentSapId = queryDepartment.SapDepartmentId,
            ResourceOwnerAzureUniqueId = queryDepartment.ResourceOwnerAzureUniqueId,
            FullDepartmentName = queryDepartment.FullDepartmentName
        };
    }
}