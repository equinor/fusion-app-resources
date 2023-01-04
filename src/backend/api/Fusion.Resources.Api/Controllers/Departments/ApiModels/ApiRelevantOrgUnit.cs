using Bogus.DataSets;
using Fusion.Resources.Domain.Models;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Fusion.Resources.Api.Controllers.Departments.ApiModels
{
    public class ApiRelevantOrgUnit
    {
        public ApiRelevantOrgUnit(QueryRelevantOrgUnit resourceOwnerProfile)
        {

            Reasons = resourceOwnerProfile.Reasons;
            Name = resourceOwnerProfile.Name;
            SapId = resourceOwnerProfile.SapId;
            ParentSapId = resourceOwnerProfile.ParentSapId;
            ShortName = resourceOwnerProfile.ShortName;
            FullDepartment = resourceOwnerProfile.FullDepartment;
            Department = resourceOwnerProfile.Department;
        }

        public string Name { get; set; }
        public string SapId { get; set; }
        public string ParentSapId { get; set; }
        public string ShortName { get; set; }
        public string Department { get; set; }
        public string FullDepartment { get; set; }

        public List<string> Reasons { get; set; } = new();


    }

}
