using System;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public class QueryResponsibilityMatrix
    {
        public QueryResponsibilityMatrix(DbResponsibilityMatrix matrix)
        {
            Id = matrix.Id;
            Project = matrix.Project == null ? null : new QueryProject(matrix.Project);
            Location = matrix.LocationId.HasValue ? new QueryLocation(matrix.LocationId.Value) : null;
            Discipline = matrix.Discipline;
            BasePosition = matrix.BasePositionId.HasValue ? new QueryBasePosition(matrix.BasePositionId.Value) : null;
            Sector = matrix.Sector;
            Unit = matrix.Unit;
            Responsible = matrix.Responsible == null ? null : new QueryPerson(matrix.Responsible);
            Created = matrix.Created;
            CreatedBy = new QueryPerson(matrix.CreatedBy);
            Updated = matrix.Updated;
            UpdatedBy = matrix.UpdatedBy == null ? null : new QueryPerson(matrix.UpdatedBy);

        }

        public Guid Id { get; set; }
        public QueryProject? Project { get; set; }
        public QueryLocation? Location { get; set; }
        public string? Discipline { get; set; }
        public QueryBasePosition? BasePosition { get; set; }
        public string? Sector { get; set; }
        public string? Unit { get; set; }
        public QueryPerson? Responsible { get; set; }
        public DateTimeOffset Created { get; set; }
        public QueryPerson CreatedBy { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public QueryPerson? UpdatedBy { get; set; }

    }

}
