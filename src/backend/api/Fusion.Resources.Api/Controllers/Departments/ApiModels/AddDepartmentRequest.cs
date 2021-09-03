using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Api.Controllers
{
    public class AddDepartmentRequest
    {
        [Required]
        public string DepartmentId { get; set; } = null!;
        public string? SectorId { get; set; }
    }
}
