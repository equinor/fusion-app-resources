using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Profile;
using Fusion.Resources.Domain;
using Fusion.Services.LineOrg.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Application
{
    public static class LineOrgExtensions
    {
        public static async Task<List<ApiDepartment>?> ResolveDepartmentChildrenAsync(this ILineOrgResolver resolver, string departmentId)
        {
            var department = await resolver.ResolveDepartmentAsync(DepartmentId.FromFullPath(departmentId));
            if (department is null) return null;

            return await resolver.ResolveDepartmentChildrenAsync(department);
        }

        public static async Task<List<ApiDepartment>?> ResolveDepartmentChildrenAsync(this ILineOrgResolver resolver, ApiDepartment department)
        {
            if (department?.Children is null) return null;

            var children = new List<ApiDepartment>();
            foreach (var child in department.Children)
            {
                var childDepartment = await resolver.ResolveDepartmentAsync(DepartmentId.FromFullPath(child.FullName));
                if (childDepartment is not null) children.Add(childDepartment);
            }

            return children;
        }

        public static async Task<List<QueryDepartment>> ToQueryDepartment(this IEnumerable<ApiDepartment> departments, IFusionProfileResolver profileResolver)
        {
            var managerIds = departments
                    .Where(x => x.Manager?.AzureUniqueId != null)
                    .Select(x => new PersonIdentifier(x.Manager!.AzureUniqueId)).ToList();

            var profiles = await profileResolver.ResolvePersonsAsync(managerIds);
            // Some profiles may be expired. Only consider existing profiles (resolved will return success).
            var profileLookup = profiles.Where(x => x.Success)
                .ToDictionary(x => x.Profile!.AzureUniqueId!.Value, x => x.Profile!);
            return departments.Select(x => new QueryDepartment(x, TryLookupManager(profileLookup, x.Manager))).ToList();
        }

        public static async Task<List<QueryDepartment>> ToQueryDepartment(this IEnumerable<ApiLineOrgUser> users, IFusionProfileResolver profileResolver)
        {
            var managerIds = users.Select(x => new PersonIdentifier(x.AzureUniqueId));
            var profiles = await profileResolver.ResolvePersonsAsync(managerIds);
            // Some profiles may be expired. Only consider existing profiles.
            var profileLookup = profiles.Where(x => x.Success).ToDictionary(x => x.Profile!.AzureUniqueId!.Value, x => x.Profile!);
            return users.Select(x => new QueryDepartment(new ApiDepartment
            {
                Name = x.Department!,
                FullName = x.FullDepartment!,
            }, TryLookupManager(profileLookup, x))).ToList();
        }

        private static FusionPersonProfile? TryLookupManager(IReadOnlyDictionary<Guid, FusionPersonProfile> profileLookup, ApiLineOrgUser? x)
        {
            return x != null && profileLookup.TryGetValue(x.AzureUniqueId, out var fusionProfile) ? fusionProfile : null;
        }
    }
}