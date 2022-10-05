using Fusion.AspNetCore.OData;
using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using Fusion.Integration.Roles.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    internal class RolesClientMock : IFusionRolesClient
    {
        private static ConcurrentDictionary<Guid, ImmutableList<FusionRoleAssignment>> roleAssignments = new();

        public Task<FusionRoleAssignment> AssignRoleAsync(Guid personAzureUniqueId, RoleAssignment role)
        {
            return AddPersonRole(personAzureUniqueId, role);
        }

        public static Task<FusionRoleAssignment> AddPersonRole(Guid personAzureUniqueId, RoleAssignment role)
        {
            var roleAssignment = new FusionRoleAssignment(Guid.NewGuid(), role.RoleName, role.Identifier, null)
            {
                Person = new RolePerson(personAzureUniqueId, "test@mail.com", "John Doe"),
                Scope = new Fusion.Integration.Roles.RoleScope(role.Scope.Type, role.Scope.Value),

                ValidTo = role.ValidTo
            };

            roleAssignments.AddOrUpdate(personAzureUniqueId,
                ImmutableList.Create(roleAssignment),
                (_, existing) => existing.Add(roleAssignment)
            );
            return Task.FromResult(roleAssignment);
        }

        public Task<FusionRoleAssignment> AssignRoleAsync(Guid personAzureUniqueId, Action<RoleAssignment> roleBuilder)
        {
            var builder = new RoleAssignment();
            roleBuilder(builder);

            return this.AssignRoleAsync(personAzureUniqueId, builder);
        }

        public Task<IEnumerable<FusionRoleAssignment>> DeleteRoleByIdentifierAsync(string externalIdentifier)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FusionRoleAssignment>> DeleteRolesAsync(Action<RolesApiODataQuery> query)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FusionRoleAssignment>> DeleteRolesAsync(PersonIdentifier person, Action<RolesApiODataQuery> query)
        {
            return Task.FromResult<IEnumerable<FusionRoleAssignment>>(new List<FusionRoleAssignment> { new FusionRoleAssignment(Guid.NewGuid(), "deleted-role", "deleted-role", null) });
        }

        public Task<FusionRoleAssignment> GetRoleByIdentifierAsync(string externalIdentifier)
        {
            return Task.FromResult(default(FusionRoleAssignment));
        }

        public Task<IEnumerable<FusionRoleAssignment>> GetRolesAsync(Action<RolesApiODataQuery> querySetup)
        {
            var query = new RolesApiODataQuery();
            querySetup(query);

            var queryString = HttpUtility.ParseQueryString(query.QueryString);
            var filterString = queryString["$filter"];
            if (!string.IsNullOrEmpty(filterString))
            {
                var odataFilter = ODataParser.Parse(filterString);

                var personFilter = odataFilter.GetFilterForField("person.id");
                var scopeFilter = odataFilter.GetFilterForField("Scope.Type");

                if (personFilter != null && personFilter.Operation == FilterOperation.Eq)
                {
                    if (!roleAssignments.TryGetValue(new Guid(personFilter.Value), out var userRoles))
                    {
                        userRoles = ImmutableList<FusionRoleAssignment>.Empty;
                    }
                    return Task.FromResult<IEnumerable<FusionRoleAssignment>>(userRoles);
                }
                else if (scopeFilter != null)
                {
                    var tmp = roleAssignments.Values.SelectMany(x => x).Where(y => y.Scope.Type == scopeFilter.Value);
                    return Task.FromResult<IEnumerable<FusionRoleAssignment>>(tmp);
                }
                throw new NotSupportedException();
            }

            return Task.FromResult<IEnumerable<FusionRoleAssignment>>(Array.Empty<FusionRoleAssignment>());
        }

        public Task<IEnumerable<FusionPersonRole>> GetUserRolesAsync(PersonIdentifier person)
        {
            if (!roleAssignments.TryGetValue(person.AzureUniquePersonId, out var userRoles))
            {
                userRoles = ImmutableList<FusionRoleAssignment>.Empty;
            }

            var userPersonRoles = new List<FusionPersonRole>();
            foreach (var roleAssignment in userRoles)
            {
                userPersonRoles.Add(new FusionPersonRole(roleAssignment.Identifier, roleAssignment.RoleName)
                {
                    Person = roleAssignment.Person,
                    Scope = roleAssignment.Scope
                });
            }

            return Task.FromResult<IEnumerable<FusionPersonRole>>(userPersonRoles);
        }

        public Task<FusionRoleAssignment> UpdateRoleAsync(Guid roleId, Action<RoleUpdateBuilder> updateRole)
        {
            throw new NotImplementedException();
        }

        public Task<FusionRoleAssignment> UpdateRoleByIdentifierAsync(string externalIdentifier, Action<RoleUpdateBuilder> updateRole)
        {
            var builder = new RoleUpdateBuilder();
            updateRole(builder);

            return Task.FromResult(default(FusionRoleAssignment));
        }
    }
}