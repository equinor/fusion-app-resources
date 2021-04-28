using MediatR;
namespace Fusion.Resources.Domain.Commands.Departments
{
    public class UpdateDepartment : IRequest<QueryDepartment>
    {
        public UpdateDepartment(string departmentString, string? sectorId)
        {
            DepartmentId = departmentString;
            SectorId = sectorId;
        }

        public string DepartmentId { get; }
        public string? SectorId { get; }
    }
}
