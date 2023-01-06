namespace Fusion.Resources.Domain.Models
{
    internal class QueryOrgUnitReason
    {
        public string? FullDepartment { get; set; }
        public string Reason { get; set; }  = "";

        public bool? IsWildCard => FullDepartment?.Contains('*')  ;
    }
}