using Fusion.Resources.Database.Entities;
using System;
using Fusion.Integration.Profile;

namespace Fusion.Resources.Domain.Models
{
    public class QueryDepartmentResponsible
    {
        public QueryDepartmentResponsible() { }
        public QueryDepartmentResponsible(DbDelegatedDepartmentResponsible responsible)
        {
            AzureAdObjectId = responsible.ResponsibleAzureObjectId;
            DepartmentId = responsible.DepartmentId;
            DateFrom = responsible.DateFrom.DateTime;
            DateTo = responsible.DateTo.DateTime;
            Reason = responsible.Reason;
            CreatedDate = responsible.DateCreated;
            CreatedByAzureUniqueId = responsible.UpdatedBy;
        }


        public Guid? AzureAdObjectId { get; set; }= null!;
        public FusionPersonProfile? DelegatedResponsible { get; set; }
        public string? DepartmentId { get; set; } = null!;
        public DateTimeOffset? DateFrom { get; set;}
        public DateTimeOffset? DateTo { get; set;}
        public string? Reason { get; set;}
        public DateTimeOffset? CreatedDate { get; set;}
        public Guid? CreatedByAzureUniqueId { get; set; }
        public FusionPersonProfile? CreatedBy { get; set; }

    }
}