using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiResponsibilityMatrix
    {
        public ApiResponsibilityMatrix(QueryResponsibilityMatrix matrix)
        {
            Id = matrix.Id;
            Created = matrix.Created;
            CreatedBy = new ApiPerson(matrix.CreatedBy);
            Project = new ApiProjectReference(matrix.Project);
            Location = new ApiLocation(matrix.Location);
            Discipline = matrix.Discipline;
            BasePosition = new ApiBasePosition(matrix.BasePosition);
            Sector = matrix.Sector;
            Unit = matrix.Unit;
            Responsible = new ApiPerson(matrix.Responsible);
        }

        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public ApiPerson CreatedBy { get; set; } = null!;
        public ApiProjectReference Project { get; set; } = null!;
        public ApiLocation Location { get; set; }
        public string? Discipline { get; set; }
        public ApiBasePosition BasePosition { get; set; }
        public string? Sector { get; set; }
        public string? Unit { get; set; }
        public ApiPerson Responsible { get; set; } = null!;
    }
}