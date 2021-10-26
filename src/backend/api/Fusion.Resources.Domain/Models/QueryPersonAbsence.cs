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

            IsPrivate = absence.IsPrivate;
            TaskDetails = (absence.TaskDetails != null) ? new QueryTaskDetails(absence.TaskDetails) : null;
            if (absence.Person is not null) Person = new QueryPerson(absence.Person);
        }

        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public QueryPerson CreatedBy { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public QueryAbsenceType Type { get; set; }
        public double? AbsencePercentage { get; set; }

        public bool IsPrivate { get; set; }
        public QueryTaskDetails? TaskDetails { get; set; }
        public QueryPerson? Person { get; set; }
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
            IsPrivate = absence.IsPrivate;
            TaskDetails = absence.TaskDetails != null ? new QueryTaskDetails(absence.TaskDetails) : null;
            if (absence.Person is not null) Person = new QueryPerson(absence.Person);
        }


        public QueryPersonAbsenceBasic(QueryPersonAbsence absence)
        {
            Id = absence.Id;
            Comment = absence.Comment;
            AppliesFrom = absence.AppliesFrom;
            AppliesTo = absence.AppliesTo;
            Type = Enum.Parse<QueryAbsenceType>($"{absence.Type}", true);
            AbsencePercentage = absence.AbsencePercentage;

            IsPrivate = absence.IsPrivate;
            TaskDetails = absence.TaskDetails != null ? new QueryTaskDetails(absence.TaskDetails) : null;
            Person = absence.Person;
        }

        public Guid Id { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public QueryAbsenceType Type { get; set; }
        public double? AbsencePercentage { get; set; }
        public bool IsPrivate { get; }
        public QueryTaskDetails? TaskDetails { get; set; }
        public QueryPerson? Person { get; set; }

    }

    public class QueryTaskDetails
    {
        public QueryTaskDetails(DbOpTaskDetails taskDetails)
        {
            BasePositionId = taskDetails.BasePositionId;
            TaskName = taskDetails.TaskName;
            RoleName = taskDetails.RoleName;
            Location = taskDetails.Location;
        }
        public QueryTaskDetails(QueryTaskDetails taskDetails)
        {
            BasePositionId = taskDetails.BasePositionId;
            TaskName = taskDetails.TaskName;
            RoleName = taskDetails.RoleName;
            Location = taskDetails.Location;
        }

        public Guid? BasePositionId { get; }
        public string? TaskName { get; }
        public string RoleName { get; }
        public string? Location { get; }
    }

    public enum QueryAbsenceType
    {
        Absence, Vacation, OtherTasks
    }
}
