using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;
using static Fusion.Resources.Api.Controllers.ApiPersonAbsence;

namespace Fusion.Resources.Api.Controllers
{
    /// <summary>
    /// A public view of the additional task for a person. 
    /// This is based on the absence, but does not include the private property as it is implicit.
    /// 
    /// The comment has been elected to not be included as well, as this is labled "internal note". 
    /// This could be included in the future if it is desired to expose more details about the persons 
    /// allocation to the task.
    /// </summary>
    public class ApiPersonAdditionalTask
    {
        public ApiPersonAdditionalTask(QueryPersonAbsence absence)
        {
            Id = absence.Id;
            AppliesFrom = absence.AppliesFrom;
            AppliesTo = absence.AppliesTo;
            Workload = absence.AbsencePercentage;

            TaskDetails = (absence.TaskDetails != null) ? new ApiAdditionalTaskTaskDetails(absence.TaskDetails) : null;
        }

        public Guid Id { get; set; }
        public ApiAdditionalTaskTaskDetails? TaskDetails { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public double? Workload { get; set; }
    }

    public class ApiAdditionalTaskTaskDetails
    {
        public ApiAdditionalTaskTaskDetails(Domain.QueryTaskDetails taskDetails)
        {
            BasePositionId = taskDetails.BasePositionId;
            TaskName = taskDetails.TaskName;
            RoleName = taskDetails.RoleName;
            Location = taskDetails.Location;
        }

        public Guid? BasePositionId { get; set; }
        public string? TaskName { get; set; }
        public string? RoleName { get; set; }
        public string? Location { get; set; }
    }

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

        public static ApiPersonAbsence CreateAdditionTask(QueryPersonAbsence absence) => new ApiPersonAbsence(absence, hidePrivateNotes: false);
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
        public ApiAbsenceType Type { get; set; }
        public double? AbsencePercentage { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ApiAbsenceType { Absence, Vacation, OtherTasks }
    }
}