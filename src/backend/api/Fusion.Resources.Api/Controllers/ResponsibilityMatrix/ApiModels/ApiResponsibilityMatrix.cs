using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiResponsibilityMatrix
    {
        public ApiResponsibilityMatrix(QueryResponsibilityMatrix matrix)
        {
            Id = matrix.Id;
            Project = matrix.Project != null ? new ApiProjectReference(matrix.Project) : null;
            Location = matrix.Location != null ? new ApiLocation(matrix.Location) : null;
            Discipline = matrix.Discipline;
            BasePosition = matrix.BasePosition != null ? new ApiBasePosition(matrix.BasePosition) : null;
            Sector = matrix.Sector;
            Unit = matrix.Unit;
            Responsible = matrix.Responsible != null ? new ApiPerson(matrix.Responsible) : null;
            
            Created = matrix.Created;
            CreatedBy = new ApiPerson(matrix.CreatedBy);
            Updated = matrix.Updated;
            UpdatedBy = matrix.UpdatedBy == null ? null : new ApiPerson(matrix.UpdatedBy);

        }

        public Guid Id { get; set; }
        public ApiProjectReference? Project { get; set; }
        public ApiLocation? Location { get; set; }
        public string? Discipline { get; set; }
        public ApiBasePosition? BasePosition { get; set; }
        public string? Sector { get; set; }
        public string? Unit { get; set; }
        public ApiPerson? Responsible { get; set; }
        public DateTimeOffset Created { get; set; }
        public ApiPerson CreatedBy { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public ApiPerson? UpdatedBy { get; set; }
        
    }
}