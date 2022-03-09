namespace Fusion.Resources.Domain
{
    /// <summary>
    /// The approval status for the specified department.
    /// Will be calculated from parents. If 
    /// </summary>
    public class QueryDepartmentAutoApprovalStatus
    {
        public QueryDepartmentAutoApprovalStatus(Database.Entities.DbDepartmentAutoApproval approval, bool inherited)
        {
            FullDepartmentPath = approval.DepartmentFullPath;
            Inherited = inherited;
            Enabled = approval.Enabled;
            IncludeSubDepartments = approval.IncludeSubDepartments;

            if (inherited)
                EffectiveDepartmentPath = approval.DepartmentFullPath;
        }

        public string FullDepartmentPath { get; set; }
        public bool Enabled { get; set; }
        public bool IncludeSubDepartments { get; set; }


        public bool Inherited { get; set; }
        public string? EffectiveDepartmentPath { get; set; }
    }

}
