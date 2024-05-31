using Fusion.Resources.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiDepartment
    {
        public ApiDepartment(QueryDepartment department)
        {
            // Keeping the name property for backwards compatebility. Should be refactored in new endpoint or version.
            Name = department.FullDepartment;
            SapId = department.Identifier;
            Title = department.Name;

            ShortName = department.ShortName;
            FullDepartment = department.FullDepartment;
            Sector = department.SectorId;
            LineOrgResponsible = (department.LineOrgResponsible is not null) ? new ApiPerson(department.LineOrgResponsible) : null;
            DelegatedResponsibles = (department.DelegatedResourceOwners is not null) 
                ? department.DelegatedResourceOwners.Select(x => new ApiPerson(x)).ToList() 
                : null;
        }

        /// <summary>
        /// The identifier for the org unit. Will be SAP id, but could potentially be workday in the future.
        /// Using sapid as name for consistancy across. Will hopefully be maintained in workday as well.
        /// </summary>
        public string? SapId { get; set; }
        public string? Title { get; set; }
        public string? ShortName { get; set; }

        public string FullDepartment { get; set; }

        public string Name { get; set; }
        public string? Sector { get; set; }
        public ApiPerson? LineOrgResponsible { get; set; }
        public List<ApiPerson>? DelegatedResponsibles { get; set; }

    }
}
