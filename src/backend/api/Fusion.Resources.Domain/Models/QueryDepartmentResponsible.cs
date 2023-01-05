using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain.Models
{
    public class QueryDepartmentResponsible
    {
        public QueryDepartmentResponsible(DbDelegatedDepartmentResponsible responsible)
        {
            AzureAdObjectId = responsible.ResponsibleAzureObjectId;
            DepartmentId = responsible.DepartmentId;
            DateFrom = responsible.DateFrom.DateTime;
            DateTo = responsible.DateTo.DateTime;
            Reason = responsible.Reason;
        }

        public Guid AzureAdObjectId { get; }
        public string DepartmentId { get; }
        public DateTime DateFrom { get; }
        public DateTime DateTo { get; }
        public string? Reason { get; }

    }
}