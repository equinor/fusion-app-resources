using System;
using System.Text.Json.Serialization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Models;


namespace Fusion.Resources.Api.Controllers
{
    public class ApiDepartmentResponsible
    {
        public ApiDepartmentResponsible(QueryDepartmentResponsible responsible)
        {
            Name = responsible.DepartmentId;
            if (responsible.DelegatedResponsible != null)
                DelegatedResponsible = new ApiPerson(responsible.DelegatedResponsible);

            DateFrom = responsible.DateFrom;
            DateTo = responsible.DateTo;
            Reason = responsible.Reason;

            CreatedDate = responsible.CreatedDate;
            if (responsible.CreatedBy != null)
                CreatedBy = new ApiPerson(responsible.CreatedBy);
        }

        public ApiDepartmentResponsible(QueryDepartmentResponsible responsible, bool limit)
        {
            Name = responsible.DepartmentId;
            if (responsible.DelegatedResponsible != null)
                DelegatedResponsible = new ApiPerson(responsible.DelegatedResponsible);

            DateFrom = responsible.DateFrom;
            DateTo = responsible.DateTo;
            Reason = limit ? null : responsible.Reason;

            CreatedDate = responsible.CreatedDate;
            if (responsible.CreatedBy != null && !limit)
                CreatedBy = new ApiPerson(responsible.CreatedBy);
        }

        public static ApiDepartmentResponsible CreateLimitedDelegatedResponsible(QueryDepartmentResponsible responsible) => new ApiDepartmentResponsible(responsible, limit: true);


        public string? Name { get; set; } = null!;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiPerson? DelegatedResponsible { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? DateFrom { get; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? DateTo { get; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? CreatedDate { get; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiPerson? CreatedBy { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Reason { get; }

    }
}