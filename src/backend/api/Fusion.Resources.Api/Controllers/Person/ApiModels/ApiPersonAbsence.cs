using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiPersonAbsence
    {
        private ApiPersonAbsence(QueryPersonAbsence absence, bool hidePrivateNotes)
        {
            Id = absence.Id;
            Created = absence.Created;
            CreatedBy = new ApiPerson(absence.CreatedBy);
            AppliesFrom = absence.AppliesFrom;
            AppliesTo = absence.AppliesTo;
            Type = Enum.Parse<ApiAbsenceType>($"{absence.Type}", true);
            AbsencePercentage = absence.AbsencePercentage;

            IsPrivate = absence.IsPrivate;
            Comment = absence.Comment;
            TaskDetails = (absence.TaskDetails != null) ? new ApiTaskDetails(absence.TaskDetails) : null;

            if (hidePrivateNotes && absence.IsPrivate)
            {
                Comment = "Not disclosed.";
                TaskDetails = (absence.TaskDetails != null) ? ApiTaskDetails.Hidden : null;
            }
        }
        private ApiPersonAbsence(QueryPersonAbsenceBasic absence, bool hidePrivateNotes)
        {
            Id = absence.Id;
            AppliesFrom = absence.AppliesFrom;
            AppliesTo = absence.AppliesTo;
            Type = Enum.Parse<ApiAbsenceType>($"{absence.Type}", true);
            AbsencePercentage = absence.AbsencePercentage;

            IsPrivate = absence.IsPrivate;
            Comment = absence.Comment;
            TaskDetails = (absence.TaskDetails != null) ? new ApiTaskDetails(absence.TaskDetails) : null;

            if (hidePrivateNotes && absence.IsPrivate)
            {
                Comment = "Not disclosed.";
                TaskDetails = (absence.TaskDetails != null) ? ApiTaskDetails.Hidden : null;
            }
        }

        public static ApiPersonAbsence CreateWithoutConfidentialTaskInfo(QueryPersonAbsence absence) => new ApiPersonAbsence(absence, hidePrivateNotes: true);
        public static ApiPersonAbsence CreateWithoutConfidentialTaskInfo(QueryPersonAbsenceBasic absence) => new ApiPersonAbsence(absence, hidePrivateNotes: true);

        public static ApiPersonAbsence CreateWithConfidentialTaskInfo(QueryPersonAbsence absence) => new ApiPersonAbsence(absence, hidePrivateNotes: false);
        public static ApiPersonAbsence CreateWithConfidentialTaskInfo(QueryPersonAbsenceBasic absence) => new ApiPersonAbsence(absence, hidePrivateNotes: false);

        public Guid Id { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? Created { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiPerson? CreatedBy { get; set; }

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