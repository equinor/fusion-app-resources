using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain.Models
{
    public class QueryDepartmentResponsible
    {
        public QueryDepartmentResponsible() { }
        public QueryDepartmentResponsible(DbDepartmentResponsible responsible)
        {
            AzureAdObjectId = responsible.ResponsibleAzureObjectId;
            DepartmentId = responsible.DepartmentId;
            DateFrom = responsible.DateFrom;
            DateTo = responsible.DateTo;
        }

        public Guid AzureAdObjectId { get; }
        public string DepartmentId { get; }
        public DateTime DateFrom { get; }
        public DateTime DateTo { get; }
    }
}
