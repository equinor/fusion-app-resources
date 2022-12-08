using Fusion.Resources.Domain.Queries;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRelevantDepartmentProfile
    {
        public ApiRelevantDepartmentProfile(QueryRelevantDepartmentProfile? resourceOwnerProfile)
        {
          
            reason = resourceOwnerProfile?.reason;
            name = resourceOwnerProfile?.name;
            sapId = resourceOwnerProfile?.sapId;
            parentSapId = resourceOwnerProfile?.parentSapId;
            shortName = resourceOwnerProfile?.shortName;
            fullDepartment = resourceOwnerProfile?.fullDepartment;

        }

        public string? name { get; set; }
        public string? sapId { get; set; }
        public string? parentSapId { get; set; }
        public string? shortName { get; set; }
        public string? department { get; set; }
        public string? fullDepartment { get; set; }

        public List<string?> reason { get; set; }


    }


}