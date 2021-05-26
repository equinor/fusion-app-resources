using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiPersonAbsence
    {
        public ApiPersonAbsence(QueryPersonAbsence absence, bool hidePrivateNotes)
        {
            Id = absence.Id;
            Created = absence.Created;
            CreatedBy = new ApiPerson(absence.CreatedBy);
            Comment = absence.Comment;
            AppliesFrom = absence.AppliesFrom;
            AppliesTo = absence.AppliesTo;
            Type = Enum.Parse<ApiAbsenceType>($"{absence.Type}", true);
            AbsencePercentage = absence.AbsencePercentage;

            IsPrivate = absence.IsPrivate;

            if (absence.TaskDetails != null)
            {
                TaskDetails = (hidePrivateNotes && absence.IsPrivate)
                    ? ApiTaskDetails.Hidden
                    : new ApiTaskDetails(absence.TaskDetails);
            }
        }

        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public ApiPerson CreatedBy { get; set; }

        public bool IsPrivate { get; set; }

        public string? Comment { get; set; }

        public ApiTaskDetails? TaskDetails { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiAbsenceType Type { get; set; }
        public double? AbsencePercentage { get; set; }

        public enum ApiAbsenceType { Absence, Vacation, OtherTasks }
    }
}