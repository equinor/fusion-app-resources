using Fusion.Resources.Domain;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiDepartmentAutoApprovalStatus
    {
        private readonly string fullDepartmentPath;

        public ApiDepartmentAutoApprovalStatus(QueryDepartmentAutoApprovalStatus approvalStatus)
        {
            Enabled = approvalStatus.Enabled;
            Mode = approvalStatus.IncludeSubDepartments ? ApiDepartmentAutoApprovalMode.All : ApiDepartmentAutoApprovalMode.Direct;
            Inherited = approvalStatus.Inherited;
            InheritedFrom = approvalStatus.EffectiveDepartmentPath;

            fullDepartmentPath = approvalStatus.FullDepartmentPath;
        }

        public bool Enabled { get; set; }
        public ApiDepartmentAutoApprovalMode Mode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FullDepartmentPath { get; set; }

        public bool Inherited { get; set; }
        public string? InheritedFrom { get; set; }

        public ApiDepartmentAutoApprovalStatus IncludeFullDepartment()
        {
            FullDepartmentPath = fullDepartmentPath;
            return this;
        }
    }
}
