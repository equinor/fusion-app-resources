using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiTaskDetails
    {
        public static readonly ApiTaskDetails Hidden = new ApiTaskDetails
        {
            IsHidden = true,
            BasePositionId = null,
            TaskName = "Not disclosed",
            RoleName = "Not disclosed",
            Location = "Not disclosed"
        };

        public ApiTaskDetails(){}
        public ApiTaskDetails(Domain.QueryTaskDetails taskDetails)
        {
            BasePositionId = taskDetails.BasePositionId;
            TaskName = taskDetails.TaskName;
            RoleName = taskDetails.RoleName;
            Location = taskDetails.Location;
        }

        public bool IsHidden { get; set; } = false;
        public Guid? BasePositionId { get; set; }
        public string? TaskName { get; set; }
        public string? RoleName { get; set; }
        public string? Location { get; set; }
    }
}
