using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiEmploymentStatus
    {
        public ApiEmploymentStatus(QueryEmploymentStatus absence)
        {
            Id = absence.Id;
            Created = absence.Created;
            CreatedBy = new ApiPerson(absence.CreatedBy);
            Comment = absence.Comment;
            AppliesFrom = absence.AppliesFrom;
            AppliesTo = absence.AppliesTo;
            Type = Enum.Parse<ApiAbsenceType>($"{absence.Type}", true);
        }

        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public ApiPerson CreatedBy { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public ApiAbsenceType Type { get; set; }

        public enum ApiAbsenceType
        {
            Absence,
            Vacation
        }
    }
}