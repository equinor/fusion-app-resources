using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiPersonAbsenceForAnalytics
    {
       
        private ApiPersonAbsenceForAnalytics(QueryPersonAbsenceBasic absence, bool hidePrivateNotes)
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

        public static ApiPersonAbsenceForAnalytics CreateWithoutConfidentialTaskInfo(QueryPersonAbsenceBasic absence) => new(absence, hidePrivateNotes: true);

        public Guid Id { get; set; }
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