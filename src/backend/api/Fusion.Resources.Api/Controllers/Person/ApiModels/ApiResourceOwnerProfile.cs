using Fusion.Resources.Domain.Queries;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiResourceOwnerProfile
    {
        public ApiResourceOwnerProfile(QueryResourceOwnerProfile resourceOwnerProfile)
        {
            FullDepartment = resourceOwnerProfile.FullDepartment!;
            Sector = resourceOwnerProfile.Sector;
            IsResourceOwner = resourceOwnerProfile.IsDepartmentManager;
            ResponsibilityInDepartments = resourceOwnerProfile.DepartmentsWithResponsibility;
            RelevantSectors = resourceOwnerProfile.RelevantSectors;

            ChildDepartments = resourceOwnerProfile.ChildDepartments;
            SiblingDepartments = resourceOwnerProfile.SiblingDepartments;

            // Compile the relevant department list
            RelevantDepartments = ResponsibilityInDepartments
                .Union(resourceOwnerProfile.ChildDepartments ?? new ())
                .Union(resourceOwnerProfile.SiblingDepartments ?? new ())                
                .Distinct()
                .ToList();

            if (Sector is not null && !RelevantDepartments.Contains(Sector))
                RelevantDepartments.Add(Sector);
        }

        public string FullDepartment { get; set; }
        public string? Sector { get; set; }


        public bool IsResourceOwner { get; set; }

        public List<string> ResponsibilityInDepartments { get; set; } = new();

        public List<string> RelevantDepartments { get; set; } = new();
        public List<string> RelevantSectors { get; set; } = new();

        public List<string>? ChildDepartments { get; set; } 
        public List<string>? SiblingDepartments { get; set; }
    }


}