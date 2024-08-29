using Fusion.Summary.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Database.Models;

public class DbDepartment
{
    public string DepartmentSapId { get; set; } = string.Empty;
    public string FullDepartmentName { get; set; } = string.Empty;

    public List<Guid> ResourceOwnersAzureUniqueId { get; set; } = [];

    public List<Guid> DelegateResourceOwnersAzureUniqueId { get; set; } = [];

    public static DbDepartment FromQueryDepartment(QueryDepartment queryDepartment)
    {
        return new DbDepartment
        {
            DepartmentSapId = queryDepartment.SapDepartmentId,
            FullDepartmentName = queryDepartment.FullDepartmentName,
            ResourceOwnersAzureUniqueId = queryDepartment.ResourceOwnersAzureUniqueId.ToList(),
            DelegateResourceOwnersAzureUniqueId = queryDepartment.DelegateResourceOwnersAzureUniqueId.ToList()
        };
    }

    internal static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbDepartment>(department =>
        {
            department.ToTable("Departments");
            department.HasKey(d => d.DepartmentSapId);
        });
    }
}