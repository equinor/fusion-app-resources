using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Profile;
using Fusion.Resources.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Application
{
    public static class LineOrgExtensions
    {
        public static async Task<List<LineOrgDepartment>?> ResolveDepartmentChildrenAsync(this ILineOrgResolver resolver, string departmentId)
        {
            var department = await resolver.ResolveDepartmentAsync(departmentId);
            if (department is null) return null;

            return await resolver.ResolveDepartmentChildrenAsync(department);
        }

        public static async Task<List<LineOrgDepartment>?> ResolveDepartmentChildrenAsync(this ILineOrgResolver resolver, LineOrgDepartment department)
        {
            if (department?.Children is null) return null;

            var children = new List<LineOrgDepartment>();
            foreach (var child in department.Children)
            {
                var childDepartment = await resolver.ResolveDepartmentAsync(child.FullName);
                if (childDepartment is not null) children.Add(childDepartment);
            }

            return children;
        }

        public static async Task<List<QueryDepartment>> ToQueryDepartment(this IEnumerable<LineOrgDepartment> departments, IFusionProfileResolver profileResolver)
        {
            var managerIds = departments
                    .Where(x => x.Manager?.AzureUniqueId != null)
                    .Select(x => new PersonIdentifier(x.Manager!.AzureUniqueId));
            var profiles = await profileResolver.ResolvePersonsAsync(managerIds);

            var missingProfile = profiles.FirstOrDefault(x => !x.Success);
            if (missingProfile is not null)
            {
                throw new IntegrationError($"Failed to resolve resource owner.", new PersonNotFoundError(missingProfile.Identifier.ToString()));
            }

            var profileLookup = profiles.ToDictionary(x => x.Profile!.AzureUniqueId!.Value, x => x.Profile!);
            return departments.Select(x => new QueryDepartment(x, x.Manager != null ? profileLookup[x.Manager!.AzureUniqueId!] : null)).ToList();
        }

        public static async Task<List<QueryDepartment>> ToQueryDepartment(this IEnumerable<LineOrgUser> users, IFusionProfileResolver profileResolver)
        {
            var managerIds = users.Select(x => new PersonIdentifier(x.AzureUniqueId));

            var profiles = await profileResolver.ResolvePersonsAsync(managerIds);

            var missingProfile = profiles.FirstOrDefault(x => !x.Success);
            if (missingProfile is not null)
            {
                throw new IntegrationError($"Failed to resolve resource owner.", new PersonNotFoundError(missingProfile.Identifier.ToString()));
            }

            var profileLookup = profiles.ToDictionary(x => x.Profile!.AzureUniqueId!.Value, x => x.Profile!);
            return users.Select(x => new QueryDepartment(new LineOrgDepartment
            {
                Name = x.Department!,
                FullName = x.FullDepartment!,
            }, profileLookup[x.AzureUniqueId])).ToList();
        }
    }
}
