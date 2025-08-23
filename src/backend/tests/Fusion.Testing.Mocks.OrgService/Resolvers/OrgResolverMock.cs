using Fusion.Integration.Org;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Testing.Mocks.OrgService.Resolvers
{
    public class OrgResolverMock : Integration.Org.IProjectOrgResolver
    {
        public Task<IEnumerable<ApiBasePositionV2>> GetBasePositionsAsync()
        {
            return Task.FromResult(PositionBuilder.AllBasePositions.AsEnumerable());
        }

        public Task<ApiBasePositionV2> ResolveBasePositionAsync(Guid basePositionId)
        {
            var bp = PositionBuilder.AllBasePositions.FirstOrDefault(bp => bp.Id == basePositionId);
            return Task.FromResult(bp);
        }

        public Task<ApiBasePositionV2> ResolveBasePositionAsync(string name, OrgProjectType projectType)
        {
            var bp = PositionBuilder.AllBasePositions.FirstOrDefault(bp => string.Equals(name, bp.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(projectType.Name, bp.ProjectType));
            return Task.FromResult(bp);
        }

        public Task<ApiProjectContractV2> ResolveContractAsync(Guid projectId, Guid contractId)
        {
            var contract = OrgServiceMock.contracts.ContainsKey(projectId) ? OrgServiceMock.contracts[projectId].FirstOrDefault(c => c.Id == contractId) : null;
            return Task.FromResult(contract);
        }

        public Task<ApiPositionV2> ResolvePositionAsync(Guid positionId)
        {
            var position = OrgServiceMock.positions.FirstOrDefault(x => x.Id == positionId);

            return Task.FromResult(position);
        }

        public Task<IEnumerable<ApiPositionV2>> ResolvePositionsAsync(IEnumerable<Guid> positionIds)
        {
            var positions = OrgServiceMock.positions.ToList();

            var allPositions = positions.Where(p => positionIds.Contains(p.Id));
            return Task.FromResult(allPositions.ToList().AsEnumerable());
        }

        public Task<ApiProjectV2> ResolveProjectAsync(OrgProjectId projectIdentifier)
        {
            var resolvedProject = projectIdentifier.Type switch
            {
                OrgProjectId.IdentifierType.DomainId => OrgServiceMock.projects.FirstOrDefault(p => string.Equals(p.DomainId, projectIdentifier.DomainId, StringComparison.OrdinalIgnoreCase)),
                OrgProjectId.IdentifierType.Id => OrgServiceMock.projects.FirstOrDefault(p => p.ProjectId == projectIdentifier.ProjectId),
                _ => throw new NotImplementedException($"Resolving by type {projectIdentifier.Type} is not implemented in mock")
            };

            return Task.FromResult(resolvedProject);
        }
    }
}
