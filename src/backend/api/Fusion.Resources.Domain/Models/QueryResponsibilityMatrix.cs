using System;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public class QueryResponsibilityMatrix
    {
        public QueryResponsibilityMatrix(DbResponsibilityMatrix matrix)
        {
            Id = matrix.Id;
            Created = matrix.Created;
            CreatedBy = new QueryPerson(matrix.CreatedBy);
            Project = new QueryProject(matrix.Project);
            Location = new QueryLocation(matrix.LocationId);
            Discipline = matrix.Discipline;
            BasePosition = new QueryBasePosition(matrix.BasePositionId);
            Sector = matrix.Sector;
            Unit = matrix.Unit;
            Responsible = new QueryPerson(matrix.Responsible);

        }

        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public QueryPerson CreatedBy { get; set; } = null!;
        public QueryProject Project { get; set; } = null!;
        public QueryLocation Location { get; set; }
        public string? Discipline { get; set; }
        public QueryBasePosition BasePosition { get; set; }
        public string? Sector { get; set; }
        public string? Unit { get; set; }
        public QueryPerson Responsible { get; set; } = null!;

    }

}
