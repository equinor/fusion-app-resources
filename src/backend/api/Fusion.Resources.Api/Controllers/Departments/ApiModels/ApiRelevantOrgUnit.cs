using Fusion.Resources.Domain.Models;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers.Departments.ApiModels
{
    public class ApiRelevantOrgUnit
    {
        public ApiRelevantOrgUnit(QueryOrgUnit? resourceOwnerProfile)
        {

            reason = resourceOwnerProfile?.Reason;
            name = resourceOwnerProfile?.name;
            sapId = resourceOwnerProfile?.sapId;
            parentSapId = resourceOwnerProfile?.parentSapId;
            shortName = resourceOwnerProfile?.shortName;
            fullDepartment = resourceOwnerProfile?.FullDepartment;

        }

        public string? name { get; set; }
        public string? sapId { get; set; }
        public string? parentSapId { get; set; }
        public string? shortName { get; set; }
        public string? department { get; set; }
        public string? fullDepartment { get; set; }

        public string? reason { get; set; }


    }

}
