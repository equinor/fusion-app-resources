using Fusion.Integration.Profile;
using Fusion.Integration.Roles;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    internal class RolesClientMock : IFusionRolesClient
    {
        public Task<FusionRoleAssignment> AssignRoleAsync(Guid personAzureUniqueId, RoleAssignment role)
        {
            return Task.FromResult(default(FusionRoleAssignment));
        }

        public Task<FusionRoleAssignment> AssignRoleAsync(Guid personAzureUniqueId, Action<RoleAssignment> roleBuilder)
        {
            var builder = new RoleAssignment();
            roleBuilder(builder);

            return Task.FromResult(default(FusionRoleAssignment));
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
            throw new NotImplementedException();
        }

        public Task<FusionRoleAssignment> GetRoleByIdentifierAsync(string externalIdentifier)
        {
            return Task.FromResult(default(FusionRoleAssignment));
        }

        public Task<IEnumerable<FusionRoleAssignment>> GetRolesAsync(Action<RolesApiODataQuery> query)
        {
            throw new NotImplementedException();
        }

        public Task<FusionRoleAssignment> UpdateRoleAsync(Guid roleId, Action<RoleUpdateBuilder> updateRole)
        {
            throw new NotImplementedException();
        }

        public Task<FusionRoleAssignment> UpdateRoleByIdentifierAsync(string externalIdentifier, Action<RoleUpdateBuilder> updateRole)
        {
            throw new NotImplementedException();
        }
    }
}
