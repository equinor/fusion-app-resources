using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Api.Controllers.Departments
{
    public class AddDepartmentRequest
    {
        [Required]
        [MaxLength(100)]
        public string DepartmentId { get; set; } = null!;
        
        [MaxLength(100)]
        public string? SectorId { get; set; }
    }
}
