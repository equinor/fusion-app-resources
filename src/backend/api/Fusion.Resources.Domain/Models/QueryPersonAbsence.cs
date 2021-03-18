using System;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public class QueryPersonAbsence
    {
        public QueryPersonAbsence(DbPersonAbsence absence)
        {
            Id = absence.Id;
            Created = absence.Created;
            CreatedBy = new QueryPerson(absence.CreatedBy);
            Comment = absence.Comment;
            AppliesFrom = absence.AppliesFrom;
            AppliesTo = absence.AppliesTo;
            Type = Enum.Parse<QueryAbsenceType>($"{absence.Type}", true);
            AbsencePercentage = absence.AbsencePercentage;
        }

        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public QueryPerson CreatedBy { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public QueryAbsenceType Type { get; set; }
        public double? AbsencePercentage { get; set; }
    }

    /// <summary>
    /// The basic representation of the absence with out the authoring info (created etc).
    /// </summary>
    public class QueryPersonAbsenceBasic
    {
        public QueryPersonAbsenceBasic(DbPersonAbsence absence)
        {
            Id = absence.Id;
            Comment = absence.Comment;
            AppliesFrom = absence.AppliesFrom;
            AppliesTo = absence.AppliesTo;
            Type = Enum.Parse<QueryAbsenceType>($"{absence.Type}", true);
            AbsencePercentage = absence.AbsencePercentage;
        }

        public Guid Id { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public QueryAbsenceType Type { get; set; }
        public double? AbsencePercentage { get; set; }
    }


    public enum QueryAbsenceType
    {
        Absence, Vacation, OtherTasks
    }
}
