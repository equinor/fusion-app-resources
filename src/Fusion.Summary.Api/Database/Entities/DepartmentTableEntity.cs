using System.ComponentModel.DataAnnotations;

namespace Fusion.Summary.Api.Database.Entities;

public class DepartmentTableEntity
{
    [Key]
    public string DepartmentSapId { get; set; } = string.Empty;
    public Guid ResourceOwnerAzureUniqueId { get; set; }
    public string FullDepartmentName { get; set; } = string.Empty;
}
