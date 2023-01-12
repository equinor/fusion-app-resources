using System;
using System.Text.Json.Serialization;
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
            DateCreated = responsible.DateCreated;
            Reason = responsible.Reason;

            if (responsible.AssignedBy != null)
                AssignedBy= new ApiPerson(responsible.AssignedBy);

        }

        public string? Name { get; set; } = null!;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiPerson? DelegatedResponsible { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTimeOffset? DateFrom { get; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] 
        public DateTimeOffset? DateTo { get; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] 
        public DateTimeOffset? DateCreated { get; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ApiPerson? AssignedBy{ get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Reason { get; }

    }
}