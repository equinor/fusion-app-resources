namespace Fusion.Resources.Domain.Models
{
    internal class QueryOrgUnitReason
    {
        public string FullDepartment { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public bool IsWildCard => FullDepartment.Contains('*')  ;
    }
}