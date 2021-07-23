using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Api.Controllers.Departments
{
    public class UpdateDepartmentRequest
    {
        [MaxLength(100)]
        public string? SectorId { get; set; }
    }
}
