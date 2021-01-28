using System;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public class QueryEmploymentStatus
    {
        public QueryEmploymentStatus(DbPersonAbsence absence)
        {
            Id = absence.Id;
            Created = absence.Created;
            CreatedBy = new QueryPerson(absence.CreatedBy);
            Comment = absence.Comment;
            AppliesFrom = absence.AppliesFrom;
            AppliesTo = absence.AppliesTo;
            Type = Enum.Parse<QueryAbsenceType>($"{absence.Type}", true);
        }

        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public QueryPerson CreatedBy { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public QueryAbsenceType Type { get; set; }

        public static QueryEmploymentStatus? FromEntityOrDefault(DbPersonAbsence? updatedBy)
        {
            if (updatedBy != null)
                return new QueryEmploymentStatus(updatedBy);

            return null;
        }
    }

    public enum QueryAbsenceType
    {
        Absence, Vacation
    }
}
