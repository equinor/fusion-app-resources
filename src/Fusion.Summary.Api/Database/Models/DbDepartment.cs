using Fusion.Summary.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Database.Models;

public class DbDepartment
{
    public string DepartmentSapId { get; set; } = string.Empty;
    public Guid ResourceOwnerAzureUniqueId { get; set; }
    public string FullDepartmentName { get; set; } = string.Empty;

    public static DbDepartment FromQueryDepartment(QueryDepartment queryDepartment)
    {
        return new DbDepartment
        {
            DepartmentSapId = queryDepartment.SapDepartmentId,
            ResourceOwnerAzureUniqueId = queryDepartment.ResourceOwnerAzureUniqueId,
            FullDepartmentName = queryDepartment.FullDepartmentName
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