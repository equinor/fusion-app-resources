using Fusion.Summary.Api.Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Summary.Api.Database.Entities;

public class DbDepartment
{
    [Key]
    public string DepartmentSapId { get; set; } = string.Empty;
    public Guid ResourceOwnerAzureUniqueId { get; set; }
    public string FullDepartmentName { get; set; } = string.Empty;

    public static DbDepartment FromQueryDepartment(QueryDepartment queryDepartment)
    {
        return new DbDepartment
        {
            DepartmentSapId = queryDepartment.DepartmentSapId,
            ResourceOwnerAzureUniqueId = queryDepartment.ResourceOwnerAzureUniqueId,
            FullDepartmentName = queryDepartment.FullDepartmentName
        };
    }
}